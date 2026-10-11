import { useState } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { ApiError, toErrorMessage } from '../../api/client';
import { useAuth } from '../../auth/useAuth';
import { Field } from '../../components/Field';

const DEFAULT_DESTINATION = '/tickets';
const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

interface LoginLocationState {
  from?: { pathname: string; search?: string; hash?: string };
}

interface FormErrors {
  email?: string;
  password?: string;
}

/** Where the visitor was heading when they were sent here, or the ticket list. */
function destinationAfterLogin(state: unknown): string {
  const from = (state as LoginLocationState | null)?.from;

  if (!from || from.pathname === '/login') {
    return DEFAULT_DESTINATION;
  }

  return `${from.pathname}${from.search ?? ''}${from.hash ?? ''}`;
}

function validate(email: string, password: string): FormErrors {
  const errors: FormErrors = {};

  if (!emailPattern.test(email.trim())) {
    errors.email = 'Enter a valid email address.';
  }

  if (password === '') {
    errors.password = 'Enter your password.';
  }

  return errors;
}

export function LoginPage() {
  const { agent, login } = useAuth();
  const location = useLocation();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string>();

  if (agent) {
    return <Navigate to={destinationAfterLogin(location.state)} replace />;
  }

  const submit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const validationErrors = validate(email, password);
    setErrors(validationErrors);
    setSubmitError(undefined);

    if (Object.keys(validationErrors).length > 0) {
      return;
    }

    setIsSubmitting(true);

    try {
      await login(email.trim(), password);
      // Signing in changes the auth state, which renders the redirect above.
    } catch (caught) {
      setSubmitError(
        caught instanceof ApiError && caught.status === 401
          ? 'Incorrect email or password.'
          : toErrorMessage(caught, 'Could not sign in. Please try again.'),
      );
      setPassword('');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <section className="login">
      <h1>Sign in</h1>

      <form className="card form" onSubmit={submit} noValidate>
        <Field id="login-email" label="Email" error={errors.email}>
          {(fieldProps) => (
            <input
              {...fieldProps}
              type="email"
              autoComplete="username"
              value={email}
              disabled={isSubmitting}
              onChange={(event) => setEmail(event.target.value)}
            />
          )}
        </Field>

        <Field id="login-password" label="Password" error={errors.password}>
          {(fieldProps) => (
            <input
              {...fieldProps}
              type="password"
              autoComplete="current-password"
              value={password}
              disabled={isSubmitting}
              onChange={(event) => setPassword(event.target.value)}
            />
          )}
        </Field>

        {submitError && (
          <p className="field__error" role="alert">
            {submitError}
          </p>
        )}

        <div className="button-row">
          <button type="submit" className="button button--primary" disabled={isSubmitting}>
            {isSubmitting ? 'Signing in...' : 'Sign in'}
          </button>
        </div>
      </form>
    </section>
  );
}
