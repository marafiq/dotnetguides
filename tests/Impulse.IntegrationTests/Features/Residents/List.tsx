interface ResidentSummary {
  id: number;
  name: string;
  room: string;
}

interface ResidentsListProps {
  residents: ResidentSummary[];
  totalCount: number;
}

export function ResidentsList({ residents, totalCount }: ResidentsListProps) {
  return (
    <div className="residents-list">
      <h1>Residents ({totalCount})</h1>
      <table>
        <thead>
          <tr>
            <th>Name</th>
            <th>Room</th>
          </tr>
        </thead>
        <tbody>
          {residents.map((r) => (
            <tr key={r.id}>
              <td>
                <a href={`/residents/${r.id}`}>{r.name}</a>
              </td>
              <td>{r.room}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
