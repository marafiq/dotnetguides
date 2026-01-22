using System.Diagnostics;

namespace Shalimar.Razor.Tests;

/// <summary>
/// Validates that generated TSX is actually valid TypeScript/React code
/// </summary>
public static class TsxValidator
{
    private static readonly string TsConfigContent = @"{
  ""compilerOptions"": {
    ""target"": ""ES2020"",
    ""module"": ""ESNext"",
    ""jsx"": ""react"",
    ""strict"": false,
    ""esModuleInterop"": true,
    ""skipLibCheck"": true,
    ""noEmit"": true,
    ""moduleResolution"": ""node"",
    ""noImplicitAny"": false
  },
  ""include"": [""*.tsx"", ""*.d.ts""]
}";

    private static readonly string ReactTypesContent = @"
// Minimal React types for TSX validation
declare module 'react' {
  export interface CSSProperties {
    [key: string]: string | number | undefined;
  }
  export type ReactNode = any;
  export type FC<P = {}> = (props: P) => ReactNode;
  const React: { createElement: any; Fragment: any };
  export default React;
}

// React namespace for React.ReactNode usage
declare namespace React {
  type ReactNode = any;
}

// TanStack Store types
declare module '@tanstack/store' {
  export class Store<T> {
    constructor(initialState: T);
    state: T;
    setState(updater: (state: T) => T): void;
    subscribe(listener: () => void): () => void;
  }
}

declare module '@tanstack/react-store' {
  import { Store } from '@tanstack/store';
  export function useStore<T, R>(store: Store<T>, selector: (state: T) => R): R;
}

declare global {
  namespace JSX {
    interface Element {}
    interface IntrinsicElements {
      div: any; span: any; p: any; a: any; button: any;
      h1: any; h2: any; h3: any; h4: any; h5: any; h6: any;
      ul: any; ol: any; li: any; img: any; input: any;
      form: any; label: any; select: any; option: any; textarea: any;
      table: any; thead: any; tbody: any; tr: any; td: any; th: any;
      header: any; footer: any; main: any; nav: any; section: any;
      article: any; aside: any; pre: any; code: any; strong: any;
      em: any; br: any; hr: any; [elemName: string]: any;
    }
  }
}

// Allow any custom types used in props
interface ItemModel { [key: string]: any; }
interface ProductModel { [key: string]: any; }
interface UserModel { [key: string]: any; }

// Stub imports for validation - match any relative path
declare module './ProductCard' { export const ProductCard: any; export default ProductCard; }
declare module './Button' { export const Button: any; export const PrimaryButton: any; export default Button; }
declare module './utils' { export const formatDate: any; export const formatCurrency: any; }

// Wildcard module declarations for any relative imports
declare module './*' { const content: any; export = content; }
declare module '../*' { const content: any; export = content; }
";

    /// <summary>
    /// Validates TSX code using the TypeScript compiler
    /// </summary>
    public static (bool IsValid, string Error) ValidateTsx(string tsxCode, string componentName = "Test")
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"tsx-validate-{Guid.NewGuid()}");

        try
        {
            Directory.CreateDirectory(tempDir);

            // Write tsconfig.json
            File.WriteAllText(Path.Combine(tempDir, "tsconfig.json"), TsConfigContent);

            // Write minimal React types
            File.WriteAllText(Path.Combine(tempDir, "react.d.ts"), ReactTypesContent);

            // Write the TSX file
            var tsxPath = Path.Combine(tempDir, $"{componentName}.tsx");
            File.WriteAllText(tsxPath, tsxCode);

            // Run tsc
            var psi = new ProcessStartInfo
            {
                FileName = "npx",
                Arguments = "tsc --noEmit",
                WorkingDirectory = tempDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
                return (false, "Failed to start tsc");

            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit(30000); // 30 second timeout

            if (process.ExitCode == 0)
                return (true, "");

            var error = string.IsNullOrEmpty(stderr) ? stdout : stderr;
            return (false, error.Trim());
        }
        catch (Exception ex)
        {
            return (false, $"Validation error: {ex.Message}");
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }
}
