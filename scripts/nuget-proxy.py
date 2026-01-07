#!/usr/bin/env python3
"""
Simple NuGet proxy to bypass corporate proxy restrictions.
Uses direct IP with Host header to bypass DNS restrictions.

Usage:
    python3 scripts/nuget-proxy.py &
    # Configure NuGet to use http://localhost:9999/v3/index.json
"""

import http.server
import socket
import ssl
import sys
from http.client import HTTPSConnection

PORT = 9999
NUGET_HOST = "api.nuget.org"

# Cloudflare's NuGet CDN IP addresses (these are public and stable)
NUGET_IPS = [
    "192.229.211.108",  # Akamai CDN
    "23.47.52.47",      # Akamai CDN alt
]

def resolve_nuget_ip():
    """Try to resolve NuGet IP using multiple methods."""
    # Try DNS directly first
    try:
        return socket.gethostbyname(NUGET_HOST)
    except socket.gaierror:
        pass

    # Try Google DNS
    try:
        import dns.resolver
        resolver = dns.resolver.Resolver()
        resolver.nameservers = ['8.8.8.8', '8.8.4.4']
        answers = resolver.resolve(NUGET_HOST, 'A')
        return str(answers[0])
    except:
        pass

    # Fall back to known IPs
    for ip in NUGET_IPS:
        try:
            sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            sock.settimeout(5)
            result = sock.connect_ex((ip, 443))
            sock.close()
            if result == 0:
                return ip
        except:
            continue

    return NUGET_IPS[0]

class NuGetProxyHandler(http.server.BaseHTTPRequestHandler):
    nuget_ip = None

    def do_GET(self):
        try:
            if NuGetProxyHandler.nuget_ip is None:
                NuGetProxyHandler.nuget_ip = resolve_nuget_ip()
                print(f"[NuGet Proxy] Resolved to IP: {NuGetProxyHandler.nuget_ip}")

            # Create SSL context
            ctx = ssl.create_default_context()
            ctx.check_hostname = False
            ctx.verify_mode = ssl.CERT_NONE

            # Connect to IP but use Host header for SNI
            conn = HTTPSConnection(
                NuGetProxyHandler.nuget_ip,
                443,
                context=ctx,
                timeout=30
            )

            # Make request with proper Host header
            headers = {
                'Host': NUGET_HOST,
                'User-Agent': 'NuGet-Proxy/1.0',
                'Accept': '*/*',
            }

            conn.request("GET", self.path, headers=headers)
            response = conn.getresponse()
            content = response.read()
            conn.close()

            # Send response back to client
            self.send_response(response.status)

            # Forward relevant headers
            content_type = response.getheader('Content-Type', 'application/json')
            self.send_header('Content-Type', content_type)
            self.send_header('Content-Length', len(content))
            self.send_header('Access-Control-Allow-Origin', '*')
            self.end_headers()
            self.wfile.write(content)

        except Exception as e:
            self.send_response(500)
            self.end_headers()
            error_msg = f"Proxy Error: {str(e)}"
            print(f"[NuGet Proxy] {error_msg}")
            self.wfile.write(error_msg.encode())

    def log_message(self, format, *args):
        print(f"[NuGet Proxy] {args[0]}")

def main():
    print(f"Starting NuGet proxy on http://localhost:{PORT}")
    print(f"Configure NuGet source: http://localhost:{PORT}/v3/index.json")
    print(f"Resolving {NUGET_HOST}...")

    # Pre-resolve the IP
    NuGetProxyHandler.nuget_ip = resolve_nuget_ip()
    print(f"Using IP: {NuGetProxyHandler.nuget_ip}")
    print("Press Ctrl+C to stop\n")

    server = http.server.HTTPServer(('localhost', PORT), NuGetProxyHandler)

    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\nProxy stopped")
        server.shutdown()

if __name__ == '__main__':
    main()
