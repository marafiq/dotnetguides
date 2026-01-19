#!/usr/bin/env python3
from playwright.sync_api import sync_playwright
import time

with sync_playwright() as p:
    browser = p.chromium.launch(headless=True)
    page = browser.new_page(viewport={'width': 1200, 'height': 1000})
    print("Loading http://localhost:3003...")
    page.goto('http://localhost:3003', wait_until='networkidle')
    time.sleep(2)
    path = '/home/user/dotnetguides/shalimar-razor-react/docs/screenshots/real-app-final.png'
    page.screenshot(path=path, full_page=True)
    print(f"Saved: {path}")
    browser.close()
