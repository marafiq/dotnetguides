import type { CarePreferencesRequest, CareLevel, DietaryRequirement } from '../../../generated/types';

interface CarePreferencesStepProps {
  data: CarePreferencesRequest;
  onChange: (data: CarePreferencesRequest) => void;
  careLevels: readonly string[];
  dietaryOptions: readonly string[];
}

export function CarePreferencesStep({
  data,
  onChange,
  careLevels,
  dietaryOptions,
}: CarePreferencesStepProps) {
  return (
    <div className="form-step" data-testid="care-preferences-step">
      <div className="form-field">
        <label htmlFor="careLevel">Care Level *</label>
        <select
          id="careLevel"
          value={data.careLevel}
          onChange={(e) => onChange({ ...data, careLevel: e.target.value as CareLevel })}
          data-testid="input-careLevel"
        >
          {careLevels.map((level) => (
            <option key={level} value={level}>
              {level}
            </option>
          ))}
        </select>
      </div>
      <div className="form-field">
        <label>Dietary Requirements</label>
        <div className="checkbox-group" data-testid="dietary-requirements">
          {dietaryOptions.map((option) => (
            <label key={option} className="checkbox-label">
              <input
                type="checkbox"
                checked={(data.dietaryRequirements as string[]).includes(option)}
                onChange={(e) => {
                  const current = data.dietaryRequirements as DietaryRequirement[];
                  const newReqs = e.target.checked
                    ? [...current, option as DietaryRequirement]
                    : current.filter((r) => r !== option);
                  onChange({ ...data, dietaryRequirements: newReqs });
                }}
                data-testid={`checkbox-${option}`}
              />
              {option}
            </label>
          ))}
        </div>
      </div>
      <div className="form-field">
        <label htmlFor="specialInstructions">Special Instructions</label>
        <textarea
          id="specialInstructions"
          value={data.specialInstructions ?? ''}
          onChange={(e) =>
            onChange({ ...data, specialInstructions: e.target.value || null })
          }
          rows={3}
          data-testid="input-specialInstructions"
        />
      </div>
      <div className="form-field">
        <label className="checkbox-label">
          <input
            type="checkbox"
            checked={data.requiresNightChecks}
            onChange={(e) => onChange({ ...data, requiresNightChecks: e.target.checked })}
            data-testid="checkbox-requiresNightChecks"
          />
          Requires Night Checks
        </label>
      </div>
    </div>
  );
}
