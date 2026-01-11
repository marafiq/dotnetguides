import { useImpulsePage } from '@impulse/react';

// Types would be generated, but showing inline for demo
interface ResidentSummary {
  id: number;
  fullName: string;
  roomNumber: string | null;
  careLevel: string;
  age: number;
}

interface ListResidentsResponse {
  residents: ResidentSummary[];
  totalCount: number;
}

export function ResidentsList() {
  const { data, isLoading, error } = useImpulsePage<ListResidentsResponse>('/residents');

  if (isLoading) {
    return <div className="loading">Loading residents...</div>;
  }

  if (error) {
    return <div className="error">Error: {error.message}</div>;
  }

  if (!data) {
    return <div className="empty">No data available</div>;
  }

  return (
    <div className="residents-list">
      <h2>Residents ({data.totalCount})</h2>
      <table className="residents-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Room</th>
            <th>Care Level</th>
            <th>Age</th>
          </tr>
        </thead>
        <tbody>
          {data.residents.map((resident) => (
            <tr key={resident.id}>
              <td>{resident.fullName}</td>
              <td>{resident.roomNumber ?? 'Unassigned'}</td>
              <td>{resident.careLevel}</td>
              <td>{resident.age}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
