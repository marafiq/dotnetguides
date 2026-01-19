#!/usr/bin/env python3
from playwright.sync_api import sync_playwright
import time

def take_screenshot():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page(viewport={'width': 1200, 'height': 900})

        print("Loading http://localhost:3001...")
        page.goto('http://localhost:3001', wait_until='networkidle')

        # Wait for React to render
        time.sleep(2)

        # Take screenshot
        screenshot_path = '/home/user/dotnetguides/shalimar-razor-react/docs/screenshots/react-app-myprofile.png'
        page.screenshot(path=screenshot_path, full_page=True)
        print(f"Screenshot saved to: {screenshot_path}")

        browser.close()

if __name__ == '__main__':
    take_screenshot()
