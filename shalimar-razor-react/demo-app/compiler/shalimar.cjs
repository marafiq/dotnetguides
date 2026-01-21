#!/usr/bin/env node
/**
 * Shalimar Razor-React Compiler (Node.js implementation)
 * Compiles .razor files to .tsx files
 */

const fs = require('fs');
const path = require('path');

// HTML elements (lowercase) vs React components (PascalCase)
const HTML_ELEMENTS = new Set([
  'a', 'abbr', 'address', 'area', 'article', 'aside', 'audio', 'b', 'base', 'bdi', 'bdo',
  'blockquote', 'body', 'br', 'button', 'canvas', 'caption', 'cite', 'code', 'col', 'colgroup',
  'data', 'datalist', 'dd', 'del', 'details', 'dfn', 'dialog', 'div', 'dl', 'dt', 'em', 'embed',
  'fieldset', 'figcaption', 'figure', 'footer', 'form', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6',
  'head', 'header', 'hgroup', 'hr', 'html', 'i', 'iframe', 'img', 'input', 'ins', 'kbd', 'label',
  'legend', 'li', 'link', 'main', 'map', 'mark', 'menu', 'meta', 'meter', 'nav', 'noscript',
  'object', 'ol', 'optgroup', 'option', 'output', 'p', 'picture', 'pre', 'progress', 'q', 'rp',
  'rt', 'ruby', 's', 'samp', 'script', 'section', 'select', 'slot', 'small', 'source', 'span',
  'strong', 'style', 'sub', 'summary', 'sup', 'svg', 'table', 'tbody', 'td', 'template', 'textarea',
  'tfoot', 'th', 'thead', 'time', 'title', 'tr', 'track', 'u', 'ul', 'var', 'video', 'wbr',
  'path', 'circle', 'rect', 'line', 'polygon', 'polyline', 'ellipse', 'g', 'text', 'defs', 'use'
]);

// Attribute mapping from HTML to React
const ATTRIBUTE_MAP = {
  'class': 'className',
  'for': 'htmlFor',
  'tabindex': 'tabIndex',
  'readonly': 'readOnly',
  'maxlength': 'maxLength',
  'minlength': 'minLength',
  'colspan': 'colSpan',
  'rowspan': 'rowSpan',
  'autofocus': 'autoFocus',
  'autoplay': 'autoPlay'
};

// C# to TypeScript type mapping
const TYPE_MAP = {
  'string': 'string',
  'int': 'number',
  'long': 'number',
  'double': 'number',
  'float': 'number',
  'decimal': 'number',
  'bool': 'boolean',
  'DateTime': 'Date',
  'Guid': 'string'
};

/**
 * Extract @code block with proper brace matching
 */
function extractCodeBlock(content) {
  const codeStart = content.indexOf('@code');
  if (codeStart === -1) return { codeBlock: null, remaining: content };

  // Find the opening brace
  let braceStart = content.indexOf('{', codeStart);
  if (braceStart === -1) return { codeBlock: null, remaining: content };

  // Match braces to find the closing one
  let depth = 1;
  let i = braceStart + 1;
  while (i < content.length && depth > 0) {
    if (content[i] === '{') depth++;
    if (content[i] === '}') depth--;
    i++;
  }

  const codeBlock = content.slice(braceStart + 1, i - 1).trim();
  const remaining = content.slice(0, codeStart) + content.slice(i);

  return { codeBlock, remaining };
}

/**
 * Parse properties from code block
 */
function parseProperties(codeBlock) {
  const props = [];
  if (!codeBlock) return props;

  const propRegex = /\[Parameter\]\s*public\s+(\w+(?:<[^>]+>)?(?:\[\])?)\s+(\w+)\s*\{\s*get;\s*set;\s*\}(?:\s*=\s*([^;]+);)?/g;
  let match;

  while ((match = propRegex.exec(codeBlock)) !== null) {
    props.push({
      type: match[1],
      name: match[2],
      defaultValue: match[3] ? match[3].trim() : null,
      isRequired: !match[3]
    });
  }

  return props;
}

/**
 * Parse a .razor file
 */
function parseRazorFile(filePath) {
  const content = fs.readFileSync(filePath, 'utf-8');
  const fileName = path.basename(filePath, '.razor');
  const directory = path.dirname(filePath);

  // Parse directive (@server or @client)
  const directiveMatch = content.match(/^@(server|client)\s*$/m);
  const directive = directiveMatch ? directiveMatch[1] : 'default';

  // Extract code block first
  const { codeBlock, remaining } = extractCodeBlock(content);

  // Parse props from code block
  const props = parseProperties(codeBlock);

  // Extract template (remove directive and imports)
  let template = remaining;
  template = template.replace(/^@(server|client)\s*$/gm, '');
  template = template.replace(/^@using\s+.+$/gm, '');
  template = template.trim();

  // Find child components
  const children = [];
  const tagRegex = /<([A-Z][a-zA-Z0-9]*)\s*[^>]*\/?>/g;
  let tagMatch;
  while ((tagMatch = tagRegex.exec(template)) !== null) {
    const tagName = tagMatch[1];
    if (!HTML_ELEMENTS.has(tagName.toLowerCase())) {
      children.push(tagName);
    }
  }

  return {
    name: fileName,
    filePath,
    directory,
    directive,
    props,
    children: [...new Set(children)],
    template
  };
}

/**
 * Convert C# type to TypeScript type
 */
function convertType(csharpType) {
  if (TYPE_MAP[csharpType]) return TYPE_MAP[csharpType];
  if (csharpType.startsWith('List<')) {
    const innerType = csharpType.slice(5, -1);
    return `${convertType(innerType)}[]`;
  }
  if (csharpType.endsWith('[]')) {
    return `${convertType(csharpType.slice(0, -2))}[]`;
  }
  if (csharpType.endsWith('?')) {
    return `${convertType(csharpType.slice(0, -1))} | null`;
  }
  return csharpType;
}

/**
 * Transform Razor template to JSX
 */
function transformToJsx(template, propNames) {
  let result = template;

  // Transform @Props.X to {X} (destructured)
  result = result.replace(/@Props\.(\w+)/g, '{$1}');

  // Transform @if blocks with nested braces
  result = transformIfBlocks(result);

  // Transform @foreach blocks
  result = transformForeachBlocks(result);

  // Transform class to className
  for (const [html, react] of Object.entries(ATTRIBUTE_MAP)) {
    result = result.replace(new RegExp(`\\b${html}=`, 'gi'), `${react}=`);
  }

  // Transform style strings to objects
  result = result.replace(/style="([^"]+)"/g, (match, styleString) => {
    const parts = styleString.split(';').filter(Boolean);
    const styleObj = parts.map(part => {
      const colonIdx = part.indexOf(':');
      if (colonIdx === -1) return null;
      const prop = part.slice(0, colonIdx).trim();
      const value = part.slice(colonIdx + 1).trim();
      const camelProp = prop.replace(/-([a-z])/g, (m, c) => c.toUpperCase());
      return `${camelProp}: '${value}'`;
    }).filter(Boolean).join(', ');
    return `style={{${styleObj}}}`;
  });

  // Transform remaining @variable to {variable}
  result = result.replace(/@(\w+)/g, '{$1}');

  // Transform attribute values that are JSX expressions: attr="{Value}" -> attr={Value}
  result = result.replace(/(\w+)="\{([^}]+)\}"/g, '$1={$2}');

  return result;
}

/**
 * Transform @if blocks with proper brace matching
 */
function transformIfBlocks(template) {
  const ifRegex = /@if\s*\(([^)]+)\)\s*\{/g;
  let result = template;
  let match;

  while ((match = ifRegex.exec(result)) !== null) {
    const condition = match[1].trim()
      .replace(/Props\./g, '')
      .replace(/ != null/g, '')
      .replace(/ == null/g, ' === null')
      .replace(/ == /g, ' === ')
      .replace(/ != /g, ' !== ');

    const startIdx = match.index;
    const braceStart = match.index + match[0].length - 1;

    // Find matching closing brace
    let depth = 1;
    let i = braceStart + 1;
    while (i < result.length && depth > 0) {
      if (result[i] === '{') depth++;
      if (result[i] === '}') depth--;
      i++;
    }

    const content = result.slice(braceStart + 1, i - 1).trim();
    const replacement = `{(${condition}) && (\n      ${content}\n    )}`;

    result = result.slice(0, startIdx) + replacement + result.slice(i);
    ifRegex.lastIndex = startIdx + replacement.length;
  }

  return result;
}

/**
 * Transform @foreach blocks
 */
function transformForeachBlocks(template) {
  const foreachRegex = /@foreach\s*\(\s*var\s+(\w+)\s+in\s+(\w+)\s*\)\s*\{/g;
  let result = template;
  let match;

  while ((match = foreachRegex.exec(result)) !== null) {
    const itemVar = match[1];
    const collection = match[2];

    const startIdx = match.index;
    const braceStart = match.index + match[0].length - 1;

    // Find matching closing brace
    let depth = 1;
    let i = braceStart + 1;
    while (i < result.length && depth > 0) {
      if (result[i] === '{') depth++;
      if (result[i] === '}') depth--;
      i++;
    }

    let content = result.slice(braceStart + 1, i - 1).trim();
    // Transform @item.Property to {item.property}
    content = content.replace(new RegExp(`@${itemVar}\\.(\\w+)`, 'g'), `{${itemVar}.$1}`);

    const replacement = `{${collection}.map((${itemVar}, index) => (\n      ${content}\n    ))}`;

    result = result.slice(0, startIdx) + replacement + result.slice(i);
    foreachRegex.lastIndex = startIdx + replacement.length;
  }

  return result;
}

/**
 * Emit TSX for a component
 */
function emitTsx(component) {
  const lines = [];

  // Imports
  lines.push("import React from 'react';");

  // Import child components
  for (const child of component.children) {
    lines.push(`import { ${child} } from './${child}';`);
  }

  if (component.children.length > 0) lines.push('');

  // Props interface
  if (component.props.length > 0) {
    lines.push('');
    lines.push(`export interface ${component.name}Props {`);
    for (const prop of component.props) {
      const tsType = convertType(prop.type);
      const optional = prop.isRequired ? '' : '?';
      lines.push(`  ${prop.name}${optional}: ${tsType};`);
    }
    lines.push('}');
  }

  lines.push('');

  // Component function
  const propsParam = component.props.length > 0
    ? `{ ${component.props.map(p => p.name).join(', ')} }: ${component.name}Props`
    : '';

  lines.push(`export function ${component.name}(${propsParam}) {`);

  const propNames = component.props.map(p => p.name);
  const jsx = transformToJsx(component.template, propNames);

  lines.push('  return (');
  lines.push(`    ${jsx.split('\n').join('\n    ')}`);
  lines.push('  );');
  lines.push('}');

  return lines.join('\n');
}

/**
 * Compile a .razor file to .tsx
 */
function compileFile(razorPath) {
  console.log(`  Compiling: ${path.basename(razorPath)}`);

  const component = parseRazorFile(razorPath);
  const tsx = emitTsx(component);
  const tsxPath = razorPath.replace('.razor', '.tsx');

  fs.writeFileSync(tsxPath, tsx);
  console.log(`    -> ${path.basename(tsxPath)} (${component.directive}, ${component.props.length} props)`);

  return { razorPath, tsxPath, component };
}

/**
 * Compile all .razor files in a directory
 */
function compileDirectory(dir) {
  console.log(`\n[Shalimar] Compiling Razor files in: ${dir}\n`);

  const results = [];

  function walkDir(currentDir) {
    const files = fs.readdirSync(currentDir);
    for (const file of files) {
      const filePath = path.join(currentDir, file);
      const stat = fs.statSync(filePath);

      if (stat.isDirectory()) {
        walkDir(filePath);
      } else if (file.endsWith('.razor')) {
        results.push(compileFile(filePath));
      }
    }
  }

  walkDir(dir);

  console.log(`\n[Shalimar] Compiled ${results.length} files\n`);
  return results;
}

// Main execution
if (require.main === module) {
  const args = process.argv.slice(2);
  const sourceDir = args[0] || './Features';

  if (!fs.existsSync(sourceDir)) {
    console.error(`Error: Source directory not found: ${sourceDir}`);
    process.exit(1);
  }

  compileDirectory(sourceDir);
}

module.exports = { parseRazorFile, emitTsx, compileFile, compileDirectory };
