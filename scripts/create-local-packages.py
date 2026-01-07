#!/usr/bin/env python3
"""
Creates local NuGet packages from SDK assemblies.
These packages are compatible with the NuGet package format.
"""

import os
import zipfile
import xml.etree.ElementTree as ET
from pathlib import Path

SDK_PATH = Path.home() / ".dotnet/sdk/10.0.101"
ROSLYN_PATH = SDK_PATH / "Roslyn/bincore"
NET10_REF_PATH = Path.home() / ".dotnet/packs/Microsoft.NETCore.App.Ref/10.0.1/ref/net10.0"
OUTPUT_PATH = Path("/home/user/dotnetguides/packages")

def create_nuspec(package_id, version, description, dependencies=None):
    """Create a .nuspec XML string."""
    root = ET.Element("package", xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd")
    metadata = ET.SubElement(root, "metadata")

    ET.SubElement(metadata, "id").text = package_id
    ET.SubElement(metadata, "version").text = version
    ET.SubElement(metadata, "description").text = description
    ET.SubElement(metadata, "authors").text = "Microsoft"
    ET.SubElement(metadata, "requireLicenseAcceptance").text = "false"

    if dependencies:
        deps = ET.SubElement(metadata, "dependencies")
        for tfm, dep_list in dependencies.items():
            group = ET.SubElement(deps, "group", targetFramework=tfm)
            for dep_id, dep_version in dep_list:
                ET.SubElement(group, "dependency", id=dep_id, version=dep_version)

    return ET.tostring(root, encoding="unicode", xml_declaration=True)

def create_rels_file(package_id):
    """Create the required _rels/.rels file."""
    return f'''<?xml version="1.0" encoding="utf-8"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Type="http://schemas.microsoft.com/packaging/2010/07/manifest" Target="/{package_id}.nuspec" Id="R1" />
</Relationships>'''

def create_content_types():
    """Create the required [Content_Types].xml file."""
    return '''<?xml version="1.0" encoding="utf-8"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml" />
  <Default Extension="nuspec" ContentType="application/octet" />
  <Default Extension="dll" ContentType="application/octet" />
  <Default Extension="xml" ContentType="application/octet" />
</Types>'''

def create_package(package_id, version, lib_items, description, dependencies=None):
    """Create a .nupkg file.

    lib_items: dict mapping tfm -> (dll_path, dll_names)
    """
    package_name = f"{package_id}.{version}.nupkg"
    package_path = OUTPUT_PATH / package_name

    with zipfile.ZipFile(package_path, 'w', zipfile.ZIP_DEFLATED) as zf:
        # Add DLLs to lib and ref folders for each TFM
        for tfm, (dll_path, dll_names) in lib_items.items():
            for dll_name in dll_names:
                full_path = dll_path / dll_name
                if full_path.exists():
                    zf.write(full_path, f"lib/{tfm}/{dll_name}")
                    zf.write(full_path, f"ref/{tfm}/{dll_name}")
                    print(f"  Added: lib/{tfm}/{dll_name}")
                else:
                    print(f"  WARNING: {dll_name} not found at {full_path}")

        # Add nuspec
        nuspec_content = create_nuspec(package_id, version, description, dependencies)
        zf.writestr(f"{package_id}.nuspec", nuspec_content)

        # Add rels file
        zf.writestr("_rels/.rels", create_rels_file(package_id))

        # Add content types
        zf.writestr("[Content_Types].xml", create_content_types())

    print(f"Created: {package_path}")
    return package_path

def main():
    OUTPUT_PATH.mkdir(parents=True, exist_ok=True)

    # Remove old packages
    for pkg in OUTPUT_PATH.glob("*.nupkg"):
        pkg.unlink()

    print("Creating local NuGet packages from SDK assemblies...\n")

    # Microsoft.CodeAnalysis.Common (base package) - net10.0 only
    print("Creating Microsoft.CodeAnalysis.Common.4.12.0...")
    create_package(
        "Microsoft.CodeAnalysis.Common",
        "4.12.0",
        {"net10.0": (ROSLYN_PATH, ["Microsoft.CodeAnalysis.dll"])},
        "A shared package used by the .NET Compiler Platform (Roslyn)."
    )

    # Microsoft.CodeAnalysis.CSharp - net10.0 only
    print("\nCreating Microsoft.CodeAnalysis.CSharp.4.12.0...")
    create_package(
        "Microsoft.CodeAnalysis.CSharp",
        "4.12.0",
        {"net10.0": (ROSLYN_PATH, ["Microsoft.CodeAnalysis.CSharp.dll"])},
        "The C# compiler and analysis APIs.",
        dependencies={".NETCoreApp10.0": [("Microsoft.CodeAnalysis.Common", "4.12.0")]}
    )

    # Microsoft.CodeAnalysis.Analyzers (empty, just metadata)
    print("\nCreating Microsoft.CodeAnalysis.Analyzers.3.3.4...")
    create_package(
        "Microsoft.CodeAnalysis.Analyzers",
        "3.3.4",
        {},  # No DLLs
        "Analyzers for Microsoft.CodeAnalysis APIs."
    )

    print("\n✓ All packages created successfully!")
    print(f"\nPackages are in: {OUTPUT_PATH}")

    # List created packages
    print("\nCreated packages:")
    for pkg in OUTPUT_PATH.glob("*.nupkg"):
        size_kb = pkg.stat().st_size / 1024
        print(f"  {pkg.name} ({size_kb:.1f} KB)")

if __name__ == "__main__":
    main()
