interface ResidentDetailProps {
  id: number;
  name: string;
  room: string;
  admitDate: string;
  allergies: string[];
}

interface Medication {
  id: number;
  name: string;
  dosage: string;
  frequency: string;
}

interface MedicationsProps {
  medications: Medication[];
}

export function ResidentDetail({ id, name, room, admitDate, allergies }: ResidentDetailProps) {
  return (
    <div className="resident-detail">
      <a href="/residents">&larr; Back</a>
      <h1>{name}</h1>
      <dl>
        <dt>Room</dt>
        <dd>{room}</dd>
        <dt>Admitted</dt>
        <dd>{new Date(admitDate).toLocaleDateString()}</dd>
        <dt>Allergies</dt>
        <dd>{allergies.length > 0 ? allergies.join(', ') : 'None'}</dd>
      </dl>
      <div id="medications" data-impulse-deferred={`/residents/${id}/medications`}>
        Loading medications...
      </div>
    </div>
  );
}

export function Medications({ medications }: MedicationsProps) {
  if (medications.length === 0) {
    return <p>No medications</p>;
  }
  return (
    <ul className="medications">
      {medications.map((m) => (
        <li key={m.id}>
          <strong>{m.name}</strong> {m.dosage} - {m.frequency}
        </li>
      ))}
    </ul>
  );
}
