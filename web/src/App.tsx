import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AuthProvider } from './auth/AuthContext';
import { ProtectedRoute } from './auth/ProtectedRoute';
import { Layout } from './components/Layout';
import { ToastProvider } from './components/Toast';
import { CompanyPage } from './pages/CompanyPage';
import { DashboardPage } from './pages/DashboardPage';
import { DevicePage } from './pages/DevicePage';
import { LoginPage } from './pages/LoginPage';

export default function App() {
  return (
    <ToastProvider>
      <AuthProvider>
        <BrowserRouter>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route element={<ProtectedRoute />}>
              <Route element={<Layout />}>
                <Route index element={<DashboardPage />} />
                <Route path="companies/:companyId" element={<CompanyPage />} />
                <Route path="companies/:companyId/devices/:deviceId" element={<DevicePage />} />
              </Route>
            </Route>
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </BrowserRouter>
      </AuthProvider>
    </ToastProvider>
  );
}
