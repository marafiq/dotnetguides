interface DashboardProps {
  totalResidents: number;
  totalMedications: number;
  pendingTasks: number;
}

export function Dashboard({ totalResidents, totalMedications, pendingTasks }: DashboardProps) {
  return (
    <div className="dashboard">
      <h1>Dashboard</h1>
      <div className="stats">
        <StatCard label="Residents" value={totalResidents} />
        <StatCard label="Medications" value={totalMedications} />
        <StatCard label="Pending Tasks" value={pendingTasks} />
      </div>
    </div>
  );
}

function StatCard({ label, value }: { label: string; value: number }) {
  return (
    <div className="stat-card">
      <div className="stat-value">{value}</div>
      <div className="stat-label">{label}</div>
    </div>
  );
}
