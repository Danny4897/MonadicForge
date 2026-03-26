import { defineConfig } from 'vitepress'

export default defineConfig({
  title: 'MonadicLeaf',
  description: 'The structural guarantee that AI-generated C# code does not break in production — static analysis, auto-migration, and Green Score.',
  base: '/MonadicLeaf/',
  cleanUrls: true,

  head: [
    ['meta', { property: 'og:type', content: 'website' }],
    ['meta', { name: 'twitter:card', content: 'summary' }],
  ],

  themeConfig: {
    logo: '/logo.svg',
    siteTitle: 'MonadicLeaf',

    nav: [
      {
        text: 'Guide',
        items: [
          { text: 'Getting Started', link: '/getting-started' },
          { text: 'How It Works', link: '/how-it-works' },
          { text: 'Green Score', link: '/green-score' },
        ],
      },
      {
        text: 'Rules',
        items: [
          { text: 'Overview', link: '/rules/' },
          { text: 'GC001 — No bare throw', link: '/rules/gc001' },
          { text: 'GC002 — No nullable return', link: '/rules/gc002' },
          { text: 'GC003 — No unhandled Result', link: '/rules/gc003' },
          { text: 'GC004 — No bool return for failable', link: '/rules/gc004' },
          { text: 'GC005–GC010', link: '/rules/' },
        ],
      },
      {
        text: 'CLI',
        items: [
          { text: 'CLI Reference', link: '/cli' },
          { text: 'CI Integration', link: '/ci' },
        ],
      },
      {
        text: 'Ecosystem',
        items: [
          { text: 'MonadicSharp Core', link: 'https://danny4897.github.io/MonadicSharp/' },
          { text: 'NuGet', link: 'https://www.nuget.org/packages/MonadicLeaf' },
        ],
      },
    ],

    sidebar: {
      '/': [
        {
          text: 'Guide',
          items: [
            { text: 'Getting Started', link: '/getting-started' },
            { text: 'How It Works', link: '/how-it-works' },
            { text: 'Green Score', link: '/green-score' },
          ],
        },
        {
          text: 'Rules',
          items: [
            { text: 'All Rules', link: '/rules/' },
            { text: 'GC001 — No bare throw', link: '/rules/gc001' },
            { text: 'GC002 — No nullable return', link: '/rules/gc002' },
            { text: 'GC003 — No unhandled Result', link: '/rules/gc003' },
            { text: 'GC004 — No bool for failable', link: '/rules/gc004' },
          ],
        },
        {
          text: 'CLI',
          items: [
            { text: 'CLI Reference', link: '/cli' },
            { text: 'CI Integration', link: '/ci' },
          ],
        },
      ],
    },

    socialLinks: [
      { icon: 'github', link: 'https://github.com/Danny4897/MonadicLeaf' },
    ],

    search: { provider: 'local' },

    footer: {
      message: 'Released under the MIT License.',
      copyright: 'Copyright © 2024–2026 Danny4897',
    },

    outline: { level: [2, 3], label: 'On this page' },
  },

  markdown: {
    theme: { light: 'github-light', dark: 'one-dark-pro' },
  },
})
