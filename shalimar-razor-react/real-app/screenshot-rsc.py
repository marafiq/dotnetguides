#!/usr/bin/env python3
from playwright.sync_api import sync_playwright
import time

with sync_playwright() as p:
    browser = p.chromium.launch(headless=True)
    page = browser.new_page(viewport={'width': 1200, 'height': 1200})
    print("Loading http://localhost:3003/rsc...")
    page.goto('http://localhost:3003/rsc', wait_until='networkidle')
    time.sleep(3)
    page.screenshot(path='/home/user/dotnetguides/shalimar-razor-react/docs/screenshots/rsc-mode.png')
    print("Saved: /home/user/dotnetguides/shalimar-razor-react/docs/screenshots/rsc-mode.png")
    browser.close()
