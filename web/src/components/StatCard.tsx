type StatCardProps = {
  label: string;
  value: string | number;
  hint?: string;
  tone?: 'default' | 'danger' | 'ok' | 'warning';
};

export function StatCard({ label, value, hint, tone = 'default' }: StatCardProps) {
  return (
    <article className={`stat-card stat-${tone}`}>
      <span className="stat-label">{label}</span>
      <strong className="stat-value">{value}</strong>
      {hint && <span className="stat-hint">{hint}</span>}
    </article>
  );
}
