#!/usr/bin/env python3
from playwright.sync_api import sync_playwright
import time

def take_screenshot():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page(viewport={'width': 1400, 'height': 1000})

        print("Loading http://localhost:5000...")
        page.goto('http://localhost:5000', wait_until='networkidle')

        # Click the Stream button to show RSC wire format
        time.sleep(1)
        page.click('button:has-text("Stream")')
        time.sleep(2)

        # Take screenshot
        screenshot_path = '/home/user/dotnetguides/shalimar-razor-react/docs/screenshots/rsc-architecture-demo.png'
        page.screenshot(path=screenshot_path, full_page=True)
        print(f"Screenshot saved to: {screenshot_path}")

        browser.close()

if __name__ == '__main__':
    take_screenshot()
