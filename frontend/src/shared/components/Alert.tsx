type AlertProps = {
  message: string;
  variant?: "error" | "success";
};

export function Alert({ message, variant = "error" }: AlertProps) {
  return <div className={`alert-banner alert-banner--${variant}`} role="alert">{message}</div>;
}
