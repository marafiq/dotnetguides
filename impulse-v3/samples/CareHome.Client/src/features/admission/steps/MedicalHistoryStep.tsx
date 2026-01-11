import type { MedicalHistoryRequest } from '../../../generated/types';

interface MedicalHistoryStepProps {
  data: MedicalHistoryRequest;
  onChange: (data: MedicalHistoryRequest) => void;
}

export function MedicalHistoryStep({ data, onChange }: MedicalHistoryStepProps) {
  return (
    <div className="form-step" data-testid="medical-history-step">
      <div className="form-field">
        <label htmlFor="conditions">Medical Conditions</label>
        <textarea
          id="conditions"
          value={(data.conditions as string[]).join('\n')}
          onChange={(e) =>
            onChange({
              ...data,
              conditions: e.target.value.split('\n').filter(Boolean),
            })
          }
          placeholder="Enter each condition on a new line"
          rows={4}
          data-testid="input-conditions"
        />
      </div>
      <div className="form-field">
        <label htmlFor="medications">Current Medications</label>
        <textarea
          id="medications"
          value={(data.medications as string[]).join('\n')}
          onChange={(e) =>
            onChange({
              ...data,
              medications: e.target.value.split('\n').filter(Boolean),
            })
          }
          placeholder="Enter each medication on a new line"
          rows={4}
          data-testid="input-medications"
        />
      </div>
      <div className="form-field">
        <label htmlFor="allergies">Allergies</label>
        <input
          id="allergies"
          type="text"
          value={data.allergies ?? ''}
          onChange={(e) => onChange({ ...data, allergies: e.target.value || null })}
          placeholder="e.g., Penicillin, Shellfish"
          data-testid="input-allergies"
        />
      </div>
      <div className="form-field">
        <label htmlFor="primaryCarePhysician">Primary Care Physician</label>
        <input
          id="primaryCarePhysician"
          type="text"
          value={data.primaryCarePhysician ?? ''}
          onChange={(e) =>
            onChange({ ...data, primaryCarePhysician: e.target.value || null })
          }
          data-testid="input-primaryCarePhysician"
        />
      </div>
    </div>
  );
}
