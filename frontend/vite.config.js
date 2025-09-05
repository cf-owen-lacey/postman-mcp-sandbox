import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react-swc'

// No proxy; frontend will call backend directly via full URL (CORS enabled on backend)
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173
  }
})
