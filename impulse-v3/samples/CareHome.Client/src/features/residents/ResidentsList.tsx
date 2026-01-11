import { useImpulsePage } from '@impulse/react';
import type { ListResidentsResponse } from '../../generated/types';
import { RoutePaths } from '../../generated/routePaths';

export function ResidentsList() {
  const { data, isLoading, error } = useImpulsePage<ListResidentsResponse>(
    RoutePaths.ListResidents
  );

  if (isLoading) {
    return <div className="loading" data-testid="loading">Loading residents...</div>;
  }

  if (error) {
    return <div className="error" data-testid="error">Error: {error.message}</div>;
  }

  if (!data) {
    return <div className="empty" data-testid="empty">No data available</div>;
  }

  return (
    <div className="residents-list" data-testid="residents-list">
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
            <tr key={resident.id} data-testid={`resident-${resident.id}`}>
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
