import { defineConfig } from 'vitepress'

export default defineConfig({
  title: 'MonadicLeaf',
  description: 'The structural guarantee that AI-generated C# code does not break in production — static analysis, auto-migration, and Green Score.',
  base: '/MonadicLeaf/',
  cleanUrls: true,
  ignoreDeadLinks: true,

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
          {
            text: 'Core',
            items: [
              { text: 'MonadicSharp', link: 'https://danny4897.github.io/MonadicSharp/' },
              { text: 'MonadicSharp.Framework', link: 'https://danny4897.github.io/MonadicSharp.Framework/' },
            ],
          },
          {
            text: 'Extensions',
            items: [
              { text: 'MonadicSharp.AI', link: 'https://danny4897.github.io/MonadicSharp.AI/' },
              { text: 'MonadicSharp.Recovery', link: 'https://danny4897.github.io/MonadicSharp.Recovery/' },
              { text: 'MonadicSharp.Azure', link: 'https://danny4897.github.io/MonadicSharp.Azure/' },
              { text: 'MonadicSharp.DI', link: 'https://danny4897.github.io/MonadicSharp.DI/' },
            ],
          },
          {
            text: 'Tooling',
            items: [
              { text: 'MonadicLeaf', link: 'https://danny4897.github.io/MonadicLeaf/' },
              { text: 'MonadicSharp × OpenCode', link: 'https://danny4897.github.io/MonadicSharp-OpenCode/' },
              { text: 'AgentScope', link: 'https://danny4897.github.io/AgentScope/' },
            ],
          },
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
        {
          text: 'Ecosystem',
          collapsed: true,
          items: [
            { text: 'MonadicSharp ↗', link: 'https://danny4897.github.io/MonadicSharp/' },
            { text: 'MonadicSharp.Framework ↗', link: 'https://danny4897.github.io/MonadicSharp.Framework/' },
            { text: 'MonadicSharp.AI ↗', link: 'https://danny4897.github.io/MonadicSharp.AI/' },
            { text: 'MonadicSharp.Recovery ↗', link: 'https://danny4897.github.io/MonadicSharp.Recovery/' },
            { text: 'MonadicSharp.Azure ↗', link: 'https://danny4897.github.io/MonadicSharp.Azure/' },
            { text: 'MonadicSharp.DI ↗', link: 'https://danny4897.github.io/MonadicSharp.DI/' },
            { text: 'MonadicLeaf ↗', link: 'https://danny4897.github.io/MonadicLeaf/' },
            { text: 'MonadicSharp × OpenCode ↗', link: 'https://danny4897.github.io/MonadicSharp-OpenCode/' },
            { text: 'AgentScope ↗', link: 'https://danny4897.github.io/AgentScope/' },
          ],
        },
      ],
    },

    socialLinks: [
      { icon: 'github', link: 'https://github.com/Danny4897/MonadicLeaf' },
    ],

    search: { provider: 'local' },

    editLink: {
      pattern: 'https://github.com/Danny4897/MonadicLeaf/edit/main/docs/:path',
      text: 'Edit this page on GitHub',
    },


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
