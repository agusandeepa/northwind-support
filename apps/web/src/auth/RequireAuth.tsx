import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from './useAuth';

/** Layout route: signed-in visitors see the page, everyone else goes to login and is sent back after. */
export function RequireAuth() {
  const { agent } = useAuth();
  const location = useLocation();

  if (!agent) {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  return <Outlet />;
}
