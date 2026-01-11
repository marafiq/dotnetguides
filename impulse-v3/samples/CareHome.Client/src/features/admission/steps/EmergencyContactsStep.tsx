import type { EmergencyContactsRequest } from '../../../generated/types';

interface EmergencyContactsStepProps {
  data: EmergencyContactsRequest;
  onChange: (data: EmergencyContactsRequest) => void;
  errors: Record<string, string[]>;
}

export function EmergencyContactsStep({
  data,
  onChange,
  errors,
}: EmergencyContactsStepProps) {
  return (
    <div className="form-step" data-testid="emergency-contacts-step">
      <h3>Primary Contact</h3>
      <div className="form-field">
        <label htmlFor="primaryContactName">Name *</label>
        <input
          id="primaryContactName"
          type="text"
          value={data.primaryContactName}
          onChange={(e) => onChange({ ...data, primaryContactName: e.target.value })}
          data-testid="input-primaryContactName"
        />
        {errors.primaryContactName && (
          <span className="error" data-testid="error-primaryContactName">
            {errors.primaryContactName[0]}
          </span>
        )}
      </div>
      <div className="form-field">
        <label htmlFor="primaryContactPhone">Phone *</label>
        <input
          id="primaryContactPhone"
          type="tel"
          value={data.primaryContactPhone}
          onChange={(e) => onChange({ ...data, primaryContactPhone: e.target.value })}
          data-testid="input-primaryContactPhone"
        />
        {errors.primaryContactPhone && (
          <span className="error" data-testid="error-primaryContactPhone">
            {errors.primaryContactPhone[0]}
          </span>
        )}
      </div>
      <div className="form-field">
        <label htmlFor="primaryContactRelationship">Relationship *</label>
        <input
          id="primaryContactRelationship"
          type="text"
          value={data.primaryContactRelationship}
          onChange={(e) =>
            onChange({ ...data, primaryContactRelationship: e.target.value })
          }
          data-testid="input-primaryContactRelationship"
        />
        {errors.primaryContactRelationship && (
          <span className="error" data-testid="error-primaryContactRelationship">
            {errors.primaryContactRelationship[0]}
          </span>
        )}
      </div>

      <h3>Secondary Contact (Optional)</h3>
      <div className="form-field">
        <label htmlFor="secondaryContactName">Name</label>
        <input
          id="secondaryContactName"
          type="text"
          value={data.secondaryContactName ?? ''}
          onChange={(e) =>
            onChange({ ...data, secondaryContactName: e.target.value || null })
          }
          data-testid="input-secondaryContactName"
        />
      </div>
      <div className="form-field">
        <label htmlFor="secondaryContactPhone">Phone</label>
        <input
          id="secondaryContactPhone"
          type="tel"
          value={data.secondaryContactPhone ?? ''}
          onChange={(e) =>
            onChange({ ...data, secondaryContactPhone: e.target.value || null })
          }
          data-testid="input-secondaryContactPhone"
        />
      </div>
    </div>
  );
}
