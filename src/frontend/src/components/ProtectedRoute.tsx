import {
    Navigate,
    Outlet,
    useLocation,
} from 'react-router'

import { useAuth } from '../auth/useAuth'

function ProtectedRoute() {
    const { session, isInitializing } = useAuth()
    const location = useLocation()

    if (isInitializing) {
        return (
            <p role="status">
                Oturumunuz kontrol ediliyor...
            </p>
        )
    }

    if (!session) {
        return (
            <Navigate
                to="/login"
                replace
                state={{
                    from: location.pathname + location.search + location.hash,
                }}
            />
        )
    }

    return <Outlet />
}

export default ProtectedRoute