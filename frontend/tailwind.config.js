/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        leaf: {
          50:  '#f0f9f3',
          100: '#dcf1e3',
          200: '#bbe3cb',
          300: '#8dcda9',
          400: '#58b082',
          500: '#2D7D46',
          600: '#266b3c',
          700: '#1f5731',
          800: '#194428',
          900: '#143821',
        },
      },
    },
  },
  plugins: [],
}
