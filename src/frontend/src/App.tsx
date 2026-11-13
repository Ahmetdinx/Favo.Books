import {
    BrowserRouter,
    Navigate,
    Route,
    Routes,
} from 'react-router'

import './App.css'

import { AuthProvider } from './auth/AuthContext'
import AppLayout from './components/AppLayout'
import ProtectedRoute from './components/ProtectedRoute'
import OnboardingPage from './pages/OnboardingPage'
import CatalogPage from './pages/CatalogPage'
import HomePage from './pages/HomePage'
import LoginPage from './pages/LoginPage'
import NotFoundPage from './pages/NotFoundPage'
import ProfilePage from './pages/ProfilePage'
import RegisterPage from './pages/RegisterPage'
import PublicReaderPage from './pages/PublicReaderPage'
import BookDetailPage from './pages/BookDetailPage'
import './LibraryTheme.css'

function App() {
    return (
        <AuthProvider>
            <BrowserRouter basename={import.meta.env.BASE_URL}>
                <Routes>
                    <Route element={<AppLayout />}>
                        <Route path="okurlar" element={<Navigate to="/" replace />} />
                        <Route path="okur/:nickname" element={<PublicReaderPage />} />
                        <Route
                            index
                            element={<HomePage />}
                        />

                        <Route
                            path="catalog"
                            element={<CatalogPage />}
                        />

                        <Route
                            path="catalog/books/:bookId"
                            element={<BookDetailPage />}
                        />

                        <Route
                            path="login"
                            element={<LoginPage />}
                        />

                        <Route
                            path="register"
                            element={<RegisterPage />}
                        />

                        <Route
                            element={<ProtectedRoute />}
                        >
                            <Route
                                path="profile"
                                element={<ProfilePage />}
                            />
                            <Route path="baslangic" element={<OnboardingPage />} />
                        </Route>

                        <Route
                            path="*"
                            element={<NotFoundPage />}
                        />
                    </Route>
                </Routes>
            </BrowserRouter>
        </AuthProvider>
    )
}

export default App
