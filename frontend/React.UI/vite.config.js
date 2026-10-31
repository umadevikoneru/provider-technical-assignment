import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// The React app calls relative /api URLs; in development Vite forwards them to the
// ASP.NET Core API, so the browser sees one origin and no CORS setup is needed.
// Override the target with API_URL if the API runs elsewhere.
const apiUrl = process.env.API_URL ?? 'http://localhost:5264'

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': { target: apiUrl, changeOrigin: true },
    },
  },
})
