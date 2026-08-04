import { Navigate, Outlet, useLocation } from 'react-router'
import { useAuth } from '../auth/AuthContext'

function ProtectedRoute() {
    const { session } = useAuth()
    const location = useLocation()

    if (!session) {
        return (
            <Navigate
                to="/login"
                replace
                state={{ from: location.pathname }}
            />
        )
    }

    return <Outlet />
}

export default ProtectedRoute