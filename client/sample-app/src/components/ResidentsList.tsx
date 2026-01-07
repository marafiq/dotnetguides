import { Link } from '@tanstack/react-router';

export function ResidentsList() {
  // In a real app, this would come from the route loader
  const props = {
    residents: [
      { id: 1, name: 'Margaret Chen', room: 'A-101' },
      { id: 2, name: 'Robert Williams', room: 'A-102' },
      { id: 3, name: 'Dorothy Johnson', room: 'B-201' },
      { id: 4, name: 'James Brown', room: 'B-202' },
      { id: 5, name: 'Patricia Davis', room: 'C-301' },
    ],
    totalCount: 5,
    page: 1,
    pageSize: 10,
  };

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
        <h1 style={{ fontSize: '1.5rem', fontWeight: 'bold' }}>Residents</h1>
        <button style={{
          backgroundColor: '#10b981',
          color: 'white',
          padding: '0.5rem 1rem',
          borderRadius: '0.375rem',
          border: 'none',
          cursor: 'pointer'
        }}>
          + Add Resident
        </button>
      </div>

      <div style={{ backgroundColor: 'white', borderRadius: '0.5rem', boxShadow: '0 1px 3px rgba(0,0,0,0.1)', overflow: 'hidden' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead>
            <tr style={{ backgroundColor: '#f9fafb' }}>
              <th style={{ padding: '0.75rem 1rem', textAlign: 'left', fontWeight: '600', color: '#374151' }}>Name</th>
              <th style={{ padding: '0.75rem 1rem', textAlign: 'left', fontWeight: '600', color: '#374151' }}>Room</th>
              <th style={{ padding: '0.75rem 1rem', textAlign: 'right', fontWeight: '600', color: '#374151' }}>Actions</th>
            </tr>
          </thead>
          <tbody>
            {props.residents.map((resident) => (
              <tr key={resident.id} style={{ borderTop: '1px solid #e5e7eb' }}>
                <td style={{ padding: '0.75rem 1rem' }}>
                  <Link
                    to="/residents/$id"
                    params={{ id: String(resident.id) }}
                    style={{ color: '#3b82f6', textDecoration: 'none', fontWeight: '500' }}
                  >
                    {resident.name}
                  </Link>
                </td>
                <td style={{ padding: '0.75rem 1rem', color: '#6b7280' }}>{resident.room}</td>
                <td style={{ padding: '0.75rem 1rem', textAlign: 'right' }}>
                  <Link
                    to="/residents/$id"
                    params={{ id: String(resident.id) }}
                    style={{ color: '#3b82f6', textDecoration: 'none', fontSize: '0.875rem' }}
                  >
                    View Details →
                  </Link>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div style={{ marginTop: '1rem', color: '#6b7280', fontSize: '0.875rem' }}>
        Showing {props.residents.length} of {props.totalCount} residents
      </div>
    </div>
  );
}
