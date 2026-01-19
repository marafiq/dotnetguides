#!/usr/bin/env python3
from playwright.sync_api import sync_playwright
import time

with sync_playwright() as p:
    browser = p.chromium.launch(headless=True)
    page = browser.new_page(viewport={'width': 1200, 'height': 1200})

    console_messages = []
    page.on("console", lambda msg: console_messages.append(f"[{msg.type}] {msg.text}"))
    page.on("pageerror", lambda err: console_messages.append(f"[PAGE ERROR] {err}"))

    print("Loading http://localhost:3003/rsc...")
    page.goto('http://localhost:3003/rsc', wait_until='networkidle')
    time.sleep(3)

    print("\n=== Console Output ===")
    for msg in console_messages:
        print(msg)

    print("\n=== Root Content ===")
    root = page.evaluate("document.getElementById('root').innerHTML")
    print(f"Length: {len(root)}")
    if root:
        print(root[:500])

    browser.close()
