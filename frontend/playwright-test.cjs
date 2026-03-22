const { chromium } = require('playwright');

const BASE_URL = 'http://localhost:5173';
const RESULTS = [];
const CONSOLE_ERRORS = [];
const API_CALLS = [];

function log(step, status, detail) {
  const icon = status === 'PASS' ? '✓' : status === 'FAIL' ? '✗' : 'i';
  const msg = `[${status}] Step ${step}: ${detail}`;
  RESULTS.push({ step, status, detail });
  console.log(msg);
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext();
  const page = await context.newPage();

  // Capture console errors
  page.on('console', msg => {
    if (msg.type() === 'error') {
      CONSOLE_ERRORS.push(msg.text());
    }
  });

  // Capture network requests
  page.on('response', response => {
    const url = response.url();
    if (url.includes('/api/') || url.includes('localhost') && !url.includes('5173') && !url.includes('vite')) {
      API_CALLS.push({ url, status: response.status() });
    }
  });

  // ─── STEP 1: Open homepage ──────────────────────────────────────────────────
  console.log('\n── Step 1: Open http://localhost:5173 ──');
  try {
    const response = await page.goto(BASE_URL, { waitUntil: 'networkidle', timeout: 15000 });
    const httpStatus = response ? response.status() : 'unknown';
    log(1, httpStatus === 200 ? 'PASS' : 'FAIL', `HTTP ${httpStatus}`);

    // Check for MonadicLeaf in header
    const titleEl = await page.$('text=MonadicLeaf');
    if (titleEl) {
      log(1, 'PASS', 'Header "MonadicLeaf" found');
    } else {
      // Try looking for it in title or any heading
      const bodyText = await page.textContent('body');
      if (bodyText && bodyText.includes('MonadicLeaf')) {
        log(1, 'PASS', '"MonadicLeaf" found in page body');
      } else {
        log(1, 'FAIL', '"MonadicLeaf" NOT found in page');
      }
    }

    // Check for Monaco editor
    await page.waitForTimeout(2000); // Give Monaco time to init
    const monacoEl = await page.$('.monaco-editor');
    if (monacoEl) {
      log(1, 'PASS', 'Monaco editor found');
    } else {
      // Monaco might be in an iframe or different selector
      const editorArea = await page.$('[data-keybinding-context]') ||
                         await page.$('.editor-container') ||
                         await page.$('[class*="monaco"]') ||
                         await page.$('textarea.inputarea') ||
                         await page.$('.view-lines');
      if (editorArea) {
        log(1, 'PASS', 'Monaco editor found (via alternate selector)');
      } else {
        log(1, 'FAIL', 'Monaco editor NOT found');
        // Screenshot for debugging
        await page.screenshot({ path: 'step1-homepage.png' });
        console.log('  → Screenshot saved: step1-homepage.png');
      }
    }

    // Log page title
    const pageTitle = await page.title();
    console.log(`  Page title: "${pageTitle}"`);

    // Log main headings for context
    const h1Text = await page.$eval('h1', el => el.textContent).catch(() => null);
    if (h1Text) console.log(`  H1: "${h1Text}"`);

  } catch (err) {
    log(1, 'FAIL', `Error: ${err.message}`);
    await page.screenshot({ path: 'step1-error.png' });
  }

  await page.screenshot({ path: 'step1-after.png' });
  console.log('  Screenshot: step1-after.png');

  // ─── STEP 2: Navigate to /register ─────────────────────────────────────────
  console.log('\n── Step 2: Navigate to /register ──');
  try {
    await page.goto(`${BASE_URL}/register`, { waitUntil: 'networkidle', timeout: 10000 });
    const url = page.url();
    log(2, 'INFO', `Current URL: ${url}`);

    // Find email field
    const emailField = await page.$('input[type="email"]') ||
                       await page.$('input[name="email"]') ||
                       await page.$('input[placeholder*="email" i]') ||
                       await page.$('input[placeholder*="Email" i]');

    // Find password field
    const passwordField = await page.$('input[type="password"]') ||
                          await page.$('input[name="password"]') ||
                          await page.$('input[placeholder*="password" i]');

    if (!emailField) {
      log(2, 'FAIL', 'Email input field NOT found');
      const inputs = await page.$$('input');
      console.log(`  Found ${inputs.length} input(s) on page`);
      for (const inp of inputs) {
        const type = await inp.getAttribute('type');
        const name = await inp.getAttribute('name');
        const placeholder = await inp.getAttribute('placeholder');
        console.log(`    input type="${type}" name="${name}" placeholder="${placeholder}"`);
      }
      await page.screenshot({ path: 'step2-no-email.png' });
      console.log('  Screenshot: step2-no-email.png');
    } else if (!passwordField) {
      log(2, 'FAIL', 'Password input field NOT found');
    } else {
      await emailField.fill('test@monadicleaf.dev');
      await passwordField.fill('Test1234!');
      log(2, 'PASS', 'Filled email and password fields');

      // Find and click submit button
      const submitBtn = await page.$('button[type="submit"]') ||
                        await page.$('button:has-text("Register")') ||
                        await page.$('button:has-text("Sign up")') ||
                        await page.$('button:has-text("Create")') ||
                        await page.$('input[type="submit"]');

      if (!submitBtn) {
        log(2, 'FAIL', 'Submit button NOT found');
        const buttons = await page.$$('button');
        for (const btn of buttons) {
          const text = await btn.textContent();
          console.log(`  Button: "${text}"`);
        }
      } else {
        const btnText = await submitBtn.textContent();
        console.log(`  Submitting via button: "${btnText}"`);

        // Listen for navigation
        const navPromise = page.waitForNavigation({ timeout: 10000 }).catch(() => null);
        await submitBtn.click();
        await navPromise;
        await page.waitForTimeout(2000);

        const newUrl = page.url();
        log(2, 'INFO', `After submit URL: ${newUrl}`);

        if (newUrl === `${BASE_URL}/` || newUrl === BASE_URL || newUrl.endsWith('/')) {
          log(2, 'PASS', 'Redirected to "/" after registration');
        } else if (newUrl.includes('/register')) {
          // Check for error message
          const errorMsg = await page.$('[class*="error"]') ||
                           await page.$('[class*="alert"]') ||
                           await page.$('[role="alert"]');
          if (errorMsg) {
            const errText = await errorMsg.textContent();
            log(2, 'FAIL', `Still on register page, error: "${errText}"`);
          } else {
            log(2, 'FAIL', `Still on register page (${newUrl}), no error visible`);
          }
        } else {
          log(2, 'PASS', `Redirected to: ${newUrl}`);
        }
      }
    }
  } catch (err) {
    log(2, 'FAIL', `Error: ${err.message}`);
  }

  await page.screenshot({ path: 'step2-after.png' });
  console.log('  Screenshot: step2-after.png');

  // ─── STEP 3: Verify logged-in state in header ───────────────────────────────
  console.log('\n── Step 3: Verify logged-in state in header ──');
  try {
    // Navigate to home if not already there
    const currentUrl = page.url();
    if (!currentUrl.endsWith('/') && !currentUrl.endsWith(BASE_URL)) {
      await page.goto(BASE_URL, { waitUntil: 'networkidle', timeout: 10000 });
    }

    const bodyText = await page.textContent('body');

    // Check for email in header
    if (bodyText && bodyText.includes('test@monadicleaf.dev')) {
      log(3, 'PASS', 'Email "test@monadicleaf.dev" visible in page');
    } else {
      // Look for generic logged-in indicators
      const logoutBtn = await page.$('button:has-text("Logout")') ||
                        await page.$('button:has-text("Sign out")') ||
                        await page.$('a:has-text("Logout")') ||
                        await page.$('[data-testid="user-menu"]') ||
                        await page.$('[class*="avatar"]') ||
                        await page.$('[class*="user-badge"]') ||
                        await page.$('[class*="usage"]');

      if (logoutBtn) {
        log(3, 'PASS', 'Logout button / user element found — user is logged in');
      } else {
        // Check for usage badge or any "analyses" counter
        const usageBadge = await page.$('[class*="badge"]') ||
                           await page.$('[class*="quota"]') ||
                           await page.$('text=/analyses/i') ||
                           await page.$('text=/used/i');

        if (usageBadge) {
          const badgeText = await usageBadge.textContent();
          log(3, 'PASS', `Usage badge found: "${badgeText}"`);
        } else {
          log(3, 'FAIL', 'No logged-in indicator found in header');
          // Log what's in the header
          const header = await page.$('header') || await page.$('nav');
          if (header) {
            const headerText = await header.textContent();
            console.log(`  Header content: "${headerText}"`);
          }
        }
      }
    }
  } catch (err) {
    log(3, 'FAIL', `Error: ${err.message}`);
  }

  await page.screenshot({ path: 'step3-after.png' });
  console.log('  Screenshot: step3-after.png');

  // ─── STEP 4: Paste C# code into Monaco editor ───────────────────────────────
  console.log('\n── Step 4: Paste C# code into editor ──');
  const CS_CODE = `using System;
public class Example {
    public string GetName(object input) {
        try { return ((MyClass)input).Name; }
        catch (Exception ex) { Console.WriteLine(ex.Message); return string.Empty; }
    }
}`;

  try {
    // Make sure we're on the playground page
    const currentUrl = page.url();
    if (!currentUrl.endsWith('/') && !currentUrl.endsWith(BASE_URL)) {
      await page.goto(BASE_URL, { waitUntil: 'networkidle', timeout: 10000 });
      await page.waitForTimeout(2000);
    }

    // Wait for Monaco editor to be ready
    await page.waitForSelector('.monaco-editor', { timeout: 15000 }).catch(async () => {
      console.log('  Waiting for Monaco editor (extended)...');
      await page.waitForTimeout(3000);
    });

    // Click on the Monaco editor to focus it
    const monacoEditor = await page.$('.monaco-editor');
    if (monacoEditor) {
      await monacoEditor.click();
      await page.waitForTimeout(500);

      // Select all and delete existing content
      await page.keyboard.press('Control+a');
      await page.waitForTimeout(200);
      await page.keyboard.press('Delete');
      await page.waitForTimeout(200);

      // Type the code (using clipboard for reliability with special chars)
      await page.evaluate((code) => {
        navigator.clipboard.writeText(code).catch(() => {});
      }, CS_CODE);

      // Try Ctrl+V paste
      await page.keyboard.press('Control+v');
      await page.waitForTimeout(1000);

      // Check if code appears in Monaco
      const editorContent = await page.evaluate(() => {
        const models = window.monaco?.editor?.getModels?.();
        if (models && models.length > 0) {
          return models[0].getValue();
        }
        return null;
      });

      if (editorContent && editorContent.includes('GetName')) {
        log(4, 'PASS', `Code entered via clipboard paste. Length: ${editorContent.length} chars`);
      } else {
        // Try typing directly into the textarea
        const textarea = await page.$('.monaco-editor textarea.inputarea') ||
                         await page.$('.inputarea');
        if (textarea) {
          await textarea.fill('');
          await textarea.type(CS_CODE, { delay: 0 });
          await page.waitForTimeout(500);
          log(4, 'PASS', 'Code entered via textarea fill');
        } else {
          // Last resort: set via monaco API
          const set = await page.evaluate((code) => {
            const models = window.monaco?.editor?.getModels?.();
            if (models && models.length > 0) {
              models[0].setValue(code);
              return true;
            }
            return false;
          }, CS_CODE);

          if (set) {
            log(4, 'PASS', 'Code set via monaco.editor.getModels()[0].setValue()');
          } else {
            log(4, 'FAIL', 'Could not set editor content');
          }
        }
      }
    } else {
      log(4, 'FAIL', 'Monaco editor not found');
    }
  } catch (err) {
    log(4, 'FAIL', `Error: ${err.message}`);
  }

  await page.screenshot({ path: 'step4-after.png' });
  console.log('  Screenshot: step4-after.png');

  // ─── STEP 5: Click Analyze ──────────────────────────────────────────────────
  console.log('\n── Step 5: Click Analyze button ──');
  try {
    // Look for the Analyze button
    const analyzeBtn = await page.$('button:has-text("Analyze")') ||
                       await page.$('button[data-testid="analyze"]') ||
                       await page.$('[aria-label*="Analyze" i]') ||
                       await page.$('button:has-text("Run")') ||
                       await page.$('button:has-text("Submit")');

    if (analyzeBtn) {
      const btnText = await analyzeBtn.textContent();
      console.log(`  Found button: "${btnText}"`);

      // Set up a promise to wait for API response
      const apiResponsePromise = page.waitForResponse(
        resp => resp.url().includes('/api/') && resp.status() !== undefined,
        { timeout: 15000 }
      ).catch(() => null);

      await analyzeBtn.click();
      log(5, 'PASS', 'Analyze button clicked');

      const apiResp = await apiResponsePromise;
      if (apiResp) {
        log(5, 'INFO', `API response: ${apiResp.url()} → HTTP ${apiResp.status()}`);
      }

      // Wait for results to appear
      await page.waitForTimeout(5000);

    } else {
      // Try Ctrl+Enter shortcut
      console.log('  Analyze button not found, trying Ctrl+Enter...');
      const monacoEditor = await page.$('.monaco-editor');
      if (monacoEditor) {
        await monacoEditor.click();
        await page.waitForTimeout(200);
      }

      const apiResponsePromise = page.waitForResponse(
        resp => resp.url().includes('/api/') && resp.status() !== undefined,
        { timeout: 15000 }
      ).catch(() => null);

      await page.keyboard.press('Control+Enter');
      log(5, 'INFO', 'Pressed Ctrl+Enter (no Analyze button found)');

      const apiResp = await apiResponsePromise;
      if (apiResp) {
        log(5, 'INFO', `API response: ${apiResp.url()} → HTTP ${apiResp.status()}`);
      }

      await page.waitForTimeout(5000);

      // Log what buttons exist
      const buttons = await page.$$('button');
      console.log(`  Available buttons (${buttons.length}):`);
      for (const btn of buttons) {
        const text = await btn.textContent();
        console.log(`    "${text.trim()}"`);
      }
    }
  } catch (err) {
    log(5, 'FAIL', `Error: ${err.message}`);
  }

  await page.screenshot({ path: 'step5-after.png' });
  console.log('  Screenshot: step5-after.png');

  // ─── STEP 6: Check Results panel ───────────────────────────────────────────
  console.log('\n── Step 6: Check Results panel ──');
  try {
    // Wait a bit more for results to render
    await page.waitForTimeout(3000);

    const bodyText = await page.textContent('body');

    // Check for Green Score gauge
    const hasGreenScore = bodyText && (
      bodyText.toLowerCase().includes('green score') ||
      bodyText.toLowerCase().includes('greenscore') ||
      bodyText.includes('GC0') // diagnostic codes
    );

    // Check for finding cards
    const findingCards = await page.$$('[class*="finding"]') ||
                         await page.$$('[class*="diagnostic"]') ||
                         await page.$$('[class*="result"]') ||
                         await page.$$('[class*="card"]');

    // Look for gauge/score elements
    const gaugeEl = await page.$('[class*="gauge"]') ||
                    await page.$('[class*="score"]') ||
                    await page.$('[class*="GreenScore"]') ||
                    await page.$('svg circle') ||
                    await page.$('[role="meter"]');

    if (gaugeEl) {
      log(6, 'PASS', 'Gauge/score element found');
    }

    if (hasGreenScore) {
      log(6, 'PASS', '"Green Score" text found in results');
    }

    if (findingCards && findingCards.length > 0) {
      log(6, 'PASS', `Found ${findingCards.length} result/finding cards`);
    }

    // Look for specific diagnostic codes GC0xx
    const gcCodes = bodyText && bodyText.match(/GC0\d{2,3}/g);
    if (gcCodes && gcCodes.length > 0) {
      log(6, 'PASS', `Found diagnostic codes: ${[...new Set(gcCodes)].join(', ')}`);
    }

    // Check for loading/spinner (might still be loading)
    const spinner = await page.$('[class*="spinner"]') ||
                    await page.$('[class*="loading"]') ||
                    await page.$('[aria-busy="true"]');
    if (spinner) {
      log(6, 'INFO', 'Loading spinner still visible — analysis may still be running');
      // Wait more
      await page.waitForTimeout(5000);
    }

    // Check for error state
    const errorEl = await page.$('[class*="error"]') ||
                    await page.$('[role="alert"]');
    if (errorEl) {
      const errText = await errorEl.textContent();
      log(6, 'FAIL', `Error shown in results: "${errText}"`);
    }

    // If nothing found, log some of the body
    if (!hasGreenScore && !gaugeEl && !(findingCards && findingCards.length > 0) && !gcCodes) {
      log(6, 'FAIL', 'No results panel content found');
      // Print last 500 chars of body
      if (bodyText) {
        const trimmed = bodyText.replace(/\s+/g, ' ').trim();
        console.log(`  Body snippet (last 500 chars): ...${trimmed.slice(-500)}`);
      }
    }

  } catch (err) {
    log(6, 'FAIL', `Error: ${err.message}`);
  }

  await page.screenshot({ path: 'step6-after.png' });
  console.log('  Screenshot: step6-after.png');

  // ─── FINAL REPORT ──────────────────────────────────────────────────────────
  console.log('\n════════════════════════════════════════');
  console.log('FINAL REPORT');
  console.log('════════════════════════════════════════');

  // Group by step
  const byStep = {};
  for (const r of RESULTS) {
    if (!byStep[r.step]) byStep[r.step] = [];
    byStep[r.step].push(r);
  }

  const stepNames = {
    1: 'Homepage load',
    2: 'Registration',
    3: 'Logged-in state',
    4: 'Code input',
    5: 'Analyze trigger',
    6: 'Results panel',
  };

  for (const [step, entries] of Object.entries(byStep)) {
    const overallPass = entries.every(e => e.status !== 'FAIL');
    const icon = overallPass ? 'PASS' : 'FAIL';
    console.log(`\nStep ${step} (${stepNames[step]}): ${icon}`);
    for (const e of entries) {
      console.log(`  [${e.status}] ${e.detail}`);
    }
  }

  console.log('\n── Console Errors ──');
  if (CONSOLE_ERRORS.length === 0) {
    console.log('  None');
  } else {
    for (const e of CONSOLE_ERRORS) {
      console.log(`  ERROR: ${e}`);
    }
  }

  console.log('\n── API Calls ──');
  if (API_CALLS.length === 0) {
    console.log('  None captured');
  } else {
    for (const c of API_CALLS) {
      console.log(`  ${c.status} ${c.url}`);
    }
  }

  console.log('\n── Screenshots ──');
  console.log('  step1-after.png, step2-after.png, step3-after.png');
  console.log('  step4-after.png, step5-after.png, step6-after.png');

  await browser.close();
})();
