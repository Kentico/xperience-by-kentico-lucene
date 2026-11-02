---
name: design-conventions
description: Design guidance for this project — color tokens, typography, grid and layout, imagery, voice and writing conventions, and the Tailwind CSS styles workflow. Use when working on UI, styling, layout, widgets or components with a visual surface, writing user-facing copy, or answering design questions.
---

# Design conventions

## Design direction

Warm Artisan Craft meets Organic Simplicity — warm neutrals with a single orange accent, clean sans-serif type, botanical line-art. Feels like a specialty-coffee menu on quality stock: restrained, warm, confident. Flat chrome; full-bleed photography carries each page. Tone: human, approachable, never corporate.

## Colors

Source of truth: `Styles/partials/theme.css` (a Tailwind `@theme` block). Use `var(--color-<name>)` — never hard-code values. Overlays and soft inks on photos or the dark band use `rgba()` literals in place; no `color-mix()`.

| Variable | Role |
| --- | --- |
| `--color-paper` | Page background |
| `--color-card` | Card / panel surface, and the white behind product imagery |
| `--color-ink` | Body text and headings |
| `--color-ink-soft` | Secondary text, warm neutral |
| `--color-ink-inverse` | Strongest ink on dark surfaces: hover states, skip link |
| `--color-cream` | Text over imagery, ticket surface |
| `--color-accent` | Fills and large decorative shapes — **never text** |
| `--color-accent-text` | Accent for text on light backgrounds (AA-safe): links, sale price, `.chip-sale` |
| `--color-accent-on-dark` | Accent for text on the dark band (AA-safe) |
| `--color-accent-ink` | Text on accent fills |
| `--color-band` / `--color-band-ink` | Dark band surface / its ink: footer, summary card, story band, cappuccino sections |
| `--color-line` | Hairlines and dividers |
| `--color-field-border` | Input and control borders |
| `--color-error` | Form validation only |

- **The accent is the only chromatic colour.** Sale pricing uses `--color-accent-text` (original price struck through in `--color-ink-soft`); `--color-error` is reserved for validation and must not be reused as a sale or discount colour. Chips share one shape and size; tag chips use the dark band surface and `.chip-sale` is the only one that keeps the accent, so a discount reads as loudly as the card's own CTA.
- Watch the two accent variants: `--color-accent-text` on light, `--color-accent-on-dark` on `--color-band`. `--color-accent` itself fails text contrast — fills only.
- Missing a colour? Add a descriptive `--color-<purpose>` token rather than a literal. The only literals that should remain are `rgba()` overlays and soft inks layered on photography or the dark band.

## Radii, leading and layout constants

- `--radius-card` (cards, images, banners) and `--radius-btn` (buttons, badges, form fields).
- `--leading-display` / `--leading-heading` / `--leading-relaxed` / `--leading-body` — pick by role, don't inline line heights.
- `--display-weight`, `--display-tracking`, `--accent-tracking` are the display-type knobs.
- `--nav-clearance`, `--page-head-clearance` and `--sticky-top` are coupled to the fixed nav — change them together.
- There are no spacing tokens: author spacing in `px`, consistent with neighbouring rules.

## Voice & writing

- Short, sharp, human: contractions, plain language, address the reader as "you", lead with concrete value — no buzzwords (robust, synergy, cutting-edge, end-to-end; "use" not "leverage").
- Marketing headlines Title Case; body and subheadings sentence case; product names capitalized (Xperience by Kentico — never "XbyK", Content Hub, Page Builder).
- Oxford comma; dates as "October 6, 2025"; em dash for breaks, en dash for ranges, hyphen for compound adjectives.
- One clear CTA per message, strong direct verbs ("Book a demo"); standalone CTAs may be ALL CAPS; bold for emphasis only — no underlining or ALL CAPS in sentences.
- Prefer: hybrid headless, plugin, microsite, third-party, nonprofit (not pure headless, plug-in, mini-site, 3rd party, non-profit).

## Typography

- One self-hosted family (`wwwroot/Content/Fonts/`, declared in `partials/fonts.css`) — **never link font CDNs**: GT Walsheim, five weights (100/300/400/500/700), exposed as `--font-sans` and used for body and headings alike. There are no icon-font glyphs; icons are inline SVG in the markup.
- Type scale lives in `partials/theme.css` as `--text-*`, in `rem` so text follows the user's browser font-size preference — read it before changing type and don't invent new steps. The ladder is `micro < label < xs < sm < md < base < lg < h4 < h3 < h2 < h1`, with `quote` sitting between `h4` and `h3`.
- Display headings use `.display` (`partials/type.css`), which sets the family, weight, tracking and leading. Always pass heading text through `HeadingFormatter.Format`: it closes the heading with the accent period (`.accent-period`), replacing a trailing period the author typed and leaving a heading that already ends in `?`, `!` or an ellipsis alone. The period is appended as markup, not by CSS, so it works at every heading level and never doubles up. Headings are a single colour: the accent period is the only accented part.
- Colour comes from context, not from the heading level: text inherits `--color-ink`, and dark surfaces override to `--color-band-ink`.
- Links: `--color-accent-text` with a `currentColor` bottom border in prose; elsewhere `a` inherits colour and is undecorated, so give links their own affordance.

## Layout

- CSS Grid and flexbox per component — there is no column-class grid system. `.wrap` (`partials/base.css`) is the centred container; `section.block` and `.block-tight` own the vertical rhythm.
- Breakpoints are literal `@media` queries inside each partial (commonly `1080px`, `900px`, `640px`) — reuse the value the neighbouring rules already use rather than adding a new one.
- Page structure (`_DancingGoatLayout.cshtml`): a skip link, then fixed `.nav-shell` → `.nav-pill` (brand, `#nav-links`, `.nav-utils`), `<main id="main">`, `_Footer`, and an always-present `[data-cart-toast]` live-region slot fed from the `data-cart-toast-added` / `data-cart-toast-error` templates beside it. The mobile menu is driven by `.nav-shell[data-menu-open]` and the dropdowns by `[data-open]` (`Scripts/siteHeader.js`).
- Landing pages use `_LandingPageLayout.cshtml` and render `<body class="landing-page campaign">`; `partials/landing-page-shell.css` holds the shared landing header and body overrides, `partials/landing-page-campaign.css` the current campaign components (`.campaign-*`). Both classes are always present — treat them as one namespace, not as switchable variants.
- Both layouts stamp `data-page-builder` on `<html>`; `Scripts/reveal.js` reads it to skip entrance animations inside Page Builder, where widgets rendered after load would never be revealed.
- Sections and widgets both emit `section.block` + `.wrap`; the nesting is expected and absorbed by `partials/sections.css` (`.section-surface > section.block:first-child`, `.wrap .wrap`). Every widget sits inside a section, since `Program.cs` sets a default section.
- A widget or section that renders its own `section-head` heading must take the text from a `Heading`/`Title` property so editors can change it, falling back to the localized default when empty — see `ThreeColumnSection` (renders the head only when set) and the Testimonial, Cafe cards and Events widgets (localized fallback). Never hard-code the only copy of a heading in the view.

## Imagery

- Decorative assets in `wwwroot/Content/Images/`, referenced from the CSS sources as `../Images/` (relative to the compiled output in `wwwroot/Content/Styles/`).
- Photography: authentic, candid people in real settings; natural light, soft warm tones. Avoid forced smiles, white studio backgrounds, corporate poses, clipart, heavy filters.

## Accessibility

Run `npm run a11y -- <url>` (axe-core) over the pages you touched. Established conventions:

- **Motion is opt-in.** Entrance reveals and the decorative hover lift/zoom are gated behind `prefers-reduced-motion` (`partials/base.css`); add new motion inside those guards.
- **Don't let `aria-label` swallow content.** On a control whose content matters — the cart link, which contains the item-count badge — use a `.visually-hidden` span for the name instead, so both are announced.
- **Cards use a labelled overlay link** (`.ac-overlay` / `.pc-overlay`), not an `<a>` wrapped around the whole card, which would make the accessible name the entire teaser. Anything interactive inside a card needs its own stacking context to stay clickable.
- **Decorative images take `alt=""`** when an adjacent link or button already carries the name.
- **Disclosure controls** carry `aria-expanded` plus `aria-controls`, and move focus into the panel they open and back on Escape.
- **Nothing time-limited takes away the only route forward**: the add-to-cart toast is `role="status"`, cancels its auto-hide on hover or focus, and has a close button; closing it hands focus to the header cart link, so it does not fall back to the top of the page.
- **Off-screen carousel slides are `inert`** and `aria-hidden`, so only the current slide is announced.
- Strings that reach the user from JavaScript are rendered server side so they stay localizable — never hard-code English in a script. A bare string travels in a `data-*` attribute (`data-cart-error`, `data-cart-announce`, `data-event-status-label`); a fragment that carries its own markup travels in a `<template>` the script clones into place (`data-cart-toast-added` / `data-cart-toast-error`). Both pills sit in the layout rather than in the add-to-cart response, which has no language context and would render them in the default language.

## JavaScript

- Four placement tiers — never a `<script src>` inside a partial or view component, which cannot declare a section:
  - **Site chrome** (`siteHeader.js`, `reveal.js`, `toast.js`, `addToCart.js`) — `<script src>` in `_DancingGoatLayout.cshtml`.
  - **Shared helpers** (`cartRequest.js`) — `<script src>` in `_DancingGoatLayout.cshtml` ahead of every consumer, exposing their API as `window.DancingGoat.<name>`. Classic scripts cannot import each other, so this is the one sanctioned use of the namespace outside Page Builder init hooks; reach for it only when two scripts must share a contract, never to hand a page behaviour an init hook.
  - **Page behaviour** (`checkoutSummary.js`, `checkoutAddress.js`, `promotionCode.js`, `qtyStepper.js`, `productSku.js`) — `@section scripts { … }` in the top-level view. `_DancingGoatLayout` and `_Layout` both render `styles` and `scripts`; `_LandingPageLayout` renders neither, so add them there if a landing-page view ever needs one.
  - **Page Builder components** (`eventsSlider.js`) — `wwwroot/PageBuilder/Public/**`, bundled by `npm run build:bundles` into the committed `wwwroot/Content/Bundles/Public/pageComponents*.js` and emitted by `<page-builder-scripts />` on the live site *and* in the editor. Expose an `initX` on `window.DancingGoat` so the widget view can initialize instances the editor inserts after the bundle has run — that initialization call is the only script a widget view should contain.
- Modern baseline, no transpile and no polyfills: classic scripts (not modules), one IIFE per file, `"use strict"` in every file, `const`/`let`, arrow callbacks, optional chaining. `fetch`, `AbortController`, `requestSubmit` and `inert` are all assumed available.
- Progressive enhancement: the server-rendered page must work without the script, and each behaviour opts in through a `data-*` attribute rather than a class.
- Guard against double initialization with a `dataset` flag on the element (see `eventsSlider.js`, `promotionCode.js`) — never a `window` global.
- User-facing strings are server-rendered, as a `data-*` attribute or as a `<template>` the script clones — the Accessibility section says which case takes which; announce asynchronous updates in a `.visually-hidden` polite live region that already exists in the markup, outside any subtree the script replaces. When the updated text is itself visible content — the product SKU line under the buy card — that visible element carries `aria-live="polite"` instead of a separate hidden region; the rule it still keeps is that the region is server-rendered before the script runs and the script rewrites its `textContent` rather than replacing the element.

## Styles workflow

- Plain CSS (native nesting) compiled by Tailwind CSS (`npm run build:css`, or `npm run watch:css` during development) into served `Site.css` / `Landing-page.css` — edit sources under `Styles/` only, recompile after every change and commit the updated compiled CSS.
- Every top-level file in `Styles/` is a bundle entry point (`site.css` → main site, `landing-page.css` → landing pages), compiled by `Tools/css.mjs`; shared building blocks live in `Styles/partials/` and must be `@import`ed from an entry point. Cascade order = `@import` order.
- Tailwind utilities (tokens via `@theme`) are generated on demand from the Razor sources declared with `@source` in the entry points (`Views/`, `Components/`, `PageTemplates/`) — the existing styles are hand-written CSS with semantic class names; Tailwind's preflight is intentionally not imported.
- Layouts link the compiled CSS: `_DancingGoatLayout.cshtml` → `Site.css`; `_LandingPageLayout.cshtml` → `Landing-page.css`.
