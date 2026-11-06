import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import { defineConfig } from 'vite'

// Proxy: frontend i API su za browser na ISTOM origin-u (localhost:5173), pa httpOnly
// refresh-token cookie (SameSite=Strict) radi bez CORS komplikacija. U Docker-u istu ulogu ima nginx.
const api = process.env.VITE_API_URL ?? 'http://localhost:5029'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    proxy: {
      '/api': { target: api, changeOrigin: false },
      '/uploads': { target: api, changeOrigin: false }
    }
  }
})
