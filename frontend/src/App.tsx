import { BrowserRouter, Routes, Route } from 'react-router-dom'
import { AuthProvider } from './context/AuthContext'
import Header from './components/Header'
import Playground from './pages/Playground'
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'

function Layout({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex flex-col h-screen overflow-hidden bg-white">
      <Header />
      <main className="flex flex-1 overflow-hidden">
        {children}
      </main>
    </div>
  )
}

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route path="/" element={<Layout><Playground /></Layout>} />
          <Route path="/login" element={<Layout><LoginPage /></Layout>} />
          <Route path="/register" element={<Layout><RegisterPage /></Layout>} />
          <Route path="/billing" element={<Layout><div className="flex flex-1 items-center justify-center text-gray-400">Billing — coming in Step 6</div></Layout>} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  )
}
