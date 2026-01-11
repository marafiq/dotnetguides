import type { BasicInfoRequest } from '../../../generated/types';

interface BasicInfoStepProps {
  data: BasicInfoRequest;
  onChange: (data: BasicInfoRequest) => void;
  errors: Record<string, string[]>;
}

export function BasicInfoStep({ data, onChange, errors }: BasicInfoStepProps) {
  return (
    <div className="form-step" data-testid="basic-info-step">
      <div className="form-field">
        <label htmlFor="firstName">First Name *</label>
        <input
          id="firstName"
          type="text"
          value={data.firstName}
          onChange={(e) => onChange({ ...data, firstName: e.target.value })}
          data-testid="input-firstName"
        />
        {errors.firstName && (
          <span className="error" data-testid="error-firstName">
            {errors.firstName[0]}
          </span>
        )}
      </div>
      <div className="form-field">
        <label htmlFor="lastName">Last Name *</label>
        <input
          id="lastName"
          type="text"
          value={data.lastName}
          onChange={(e) => onChange({ ...data, lastName: e.target.value })}
          data-testid="input-lastName"
        />
        {errors.lastName && (
          <span className="error" data-testid="error-lastName">
            {errors.lastName[0]}
          </span>
        )}
      </div>
      <div className="form-field">
        <label htmlFor="dateOfBirth">Date of Birth *</label>
        <input
          id="dateOfBirth"
          type="date"
          value={data.dateOfBirth}
          onChange={(e) => onChange({ ...data, dateOfBirth: e.target.value })}
          data-testid="input-dateOfBirth"
        />
        {errors.dateOfBirth && (
          <span className="error" data-testid="error-dateOfBirth">
            {errors.dateOfBirth[0]}
          </span>
        )}
      </div>
      <div className="form-field">
        <label htmlFor="roomPreference">Room Preference</label>
        <input
          id="roomPreference"
          type="text"
          value={data.roomPreference ?? ''}
          onChange={(e) => onChange({ ...data, roomPreference: e.target.value || null })}
          placeholder="e.g., Ground floor, Near nurse station"
          data-testid="input-roomPreference"
        />
      </div>
    </div>
  );
}
