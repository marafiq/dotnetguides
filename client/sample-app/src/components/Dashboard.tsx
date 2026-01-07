import { Link } from '@tanstack/react-router';

export function Dashboard() {
  // In a real app, this would come from the route loader
  const props = {
    totalResidents: 5,
    totalMedications: 12,
    pendingTasks: 3,
    recentActivities: [
      { description: 'Added medication for Margaret Chen', timestamp: new Date().toISOString(), userName: 'Sarah' },
      { description: 'Updated room assignment for Robert Williams', timestamp: new Date().toISOString(), userName: 'Mike' },
    ],
  };

  return (
    <div>
      <h1 style={{ fontSize: '1.5rem', fontWeight: 'bold', marginBottom: '1.5rem' }}>Dashboard</h1>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '1.5rem', marginBottom: '2rem' }}>
        <StatCard title="Total Residents" value={props.totalResidents} color="#3b82f6" />
        <StatCard title="Active Medications" value={props.totalMedications} color="#10b981" />
        <StatCard title="Pending Tasks" value={props.pendingTasks} color="#f59e0b" />
      </div>

      <div style={{ backgroundColor: 'white', borderRadius: '0.5rem', padding: '1.5rem', boxShadow: '0 1px 3px rgba(0,0,0,0.1)' }}>
        <h2 style={{ fontSize: '1.125rem', fontWeight: '600', marginBottom: '1rem' }}>Recent Activity</h2>
        <ul style={{ listStyle: 'none', padding: 0, margin: 0 }}>
          {props.recentActivities.map((activity, i) => (
            <li key={i} style={{ padding: '0.75rem 0', borderBottom: i < props.recentActivities.length - 1 ? '1px solid #e5e5e5' : 'none' }}>
              <div style={{ fontWeight: '500' }}>{activity.description}</div>
              <div style={{ fontSize: '0.875rem', color: '#6b7280' }}>by {activity.userName}</div>
            </li>
          ))}
        </ul>
      </div>

      <div style={{ marginTop: '2rem' }}>
        <Link
          to="/residents"
          style={{
            backgroundColor: '#3b82f6',
            color: 'white',
            padding: '0.75rem 1.5rem',
            borderRadius: '0.375rem',
            textDecoration: 'none',
            display: 'inline-block'
          }}
        >
          View All Residents
        </Link>
      </div>
    </div>
  );
}

function StatCard({ title, value, color }: { title: string; value: number; color: string }) {
  return (
    <div style={{
      backgroundColor: 'white',
      borderRadius: '0.5rem',
      padding: '1.5rem',
      boxShadow: '0 1px 3px rgba(0,0,0,0.1)',
      borderLeft: `4px solid ${color}`
    }}>
      <div style={{ color: '#6b7280', fontSize: '0.875rem', marginBottom: '0.5rem' }}>{title}</div>
      <div style={{ fontSize: '2rem', fontWeight: 'bold', color }}>{value}</div>
    </div>
  );
}
