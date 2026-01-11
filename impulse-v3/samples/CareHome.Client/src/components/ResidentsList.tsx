import type { ImpulseComponentProps } from '@impulse/react';

interface Resident {
  id: number;
  firstName: string;
  lastName: string;
  room: string;
  careLevel: string;
  age: number;
}

interface ResidentsListData {
  title: string;
  residents: Resident[];
}

export function ResidentsList({ data, navigate }: ImpulseComponentProps<ResidentsListData>) {
  return (
    <div className="card" data-testid="residents-list">
      <h2>{data.title} ({data.residents.length})</h2>
      <table>
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
              <td>{resident.firstName} {resident.lastName}</td>
              <td>{resident.room}</td>
              <td>{resident.careLevel}</td>
              <td>{resident.age}</td>
            </tr>
          ))}
        </tbody>
      </table>
      <button
        className="btn-primary"
        onClick={() => navigate('/admission')}
        data-testid="nav-admission"
      >
        New Admission
      </button>
    </div>
  );
}
