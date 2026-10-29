/// <reference types="vitest/config" />
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// Em desenvolvimento, o Vite repassa as chamadas de API para o ASP.NET Core.
// Assim o front e a API parecem ter a mesma origem e não é preciso configurar CORS.
const apiUrl = process.env.API_URL ?? 'http://localhost:5259'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      '/links': { target: apiUrl, changeOrigin: true },
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
  },
})
