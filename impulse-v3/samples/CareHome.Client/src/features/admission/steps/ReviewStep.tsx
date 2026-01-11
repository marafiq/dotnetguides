import type {
  BasicInfoRequest,
  MedicalHistoryRequest,
  CarePreferencesRequest,
  EmergencyContactsRequest,
} from '../../../generated/types';

interface ReviewStepProps {
  basicInfo: BasicInfoRequest;
  medicalHistory: MedicalHistoryRequest;
  carePreferences: CarePreferencesRequest;
  emergencyContacts: EmergencyContactsRequest;
  consentGiven: boolean;
  termsAccepted: boolean;
  onConsentChange: (value: boolean) => void;
  onTermsChange: (value: boolean) => void;
  errors: Record<string, string[]>;
}

export function ReviewStep({
  basicInfo,
  medicalHistory,
  carePreferences,
  emergencyContacts,
  consentGiven,
  termsAccepted,
  onConsentChange,
  onTermsChange,
  errors,
}: ReviewStepProps) {
  return (
    <div className="review-step" data-testid="review-step">
      <div className="review-section">
        <h3>Basic Information</h3>
        <p>
          <strong>Name:</strong> {basicInfo.firstName} {basicInfo.lastName}
        </p>
        <p>
          <strong>Date of Birth:</strong> {basicInfo.dateOfBirth}
        </p>
        {basicInfo.roomPreference && (
          <p>
            <strong>Room Preference:</strong> {basicInfo.roomPreference}
          </p>
        )}
      </div>

      <div className="review-section">
        <h3>Medical History</h3>
        <p>
          <strong>Conditions:</strong>{' '}
          {(medicalHistory.conditions as string[]).length > 0
            ? (medicalHistory.conditions as string[]).join(', ')
            : 'None specified'}
        </p>
        <p>
          <strong>Medications:</strong>{' '}
          {(medicalHistory.medications as string[]).length > 0
            ? (medicalHistory.medications as string[]).join(', ')
            : 'None'}
        </p>
        <p>
          <strong>Allergies:</strong> {medicalHistory.allergies || 'None'}
        </p>
      </div>

      <div className="review-section">
        <h3>Care Preferences</h3>
        <p>
          <strong>Care Level:</strong> {carePreferences.careLevel}
        </p>
        <p>
          <strong>Dietary Requirements:</strong>{' '}
          {(carePreferences.dietaryRequirements as string[]).join(', ') || 'Regular'}
        </p>
        <p>
          <strong>Night Checks:</strong>{' '}
          {carePreferences.requiresNightChecks ? 'Yes' : 'No'}
        </p>
      </div>

      <div className="review-section">
        <h3>Emergency Contact</h3>
        <p>
          <strong>Primary:</strong> {emergencyContacts.primaryContactName} (
          {emergencyContacts.primaryContactRelationship}) -{' '}
          {emergencyContacts.primaryContactPhone}
        </p>
        {emergencyContacts.secondaryContactName && (
          <p>
            <strong>Secondary:</strong> {emergencyContacts.secondaryContactName} -{' '}
            {emergencyContacts.secondaryContactPhone}
          </p>
        )}
      </div>

      <div className="consent-section" data-testid="consent-section">
        <label className="checkbox-label">
          <input
            type="checkbox"
            checked={consentGiven}
            onChange={(e) => onConsentChange(e.target.checked)}
            data-testid="checkbox-consent"
          />
          I consent to the collection and use of this information for care purposes
        </label>
        {errors.consentGiven && (
          <span className="error" data-testid="error-consentGiven">
            {errors.consentGiven[0]}
          </span>
        )}

        <label className="checkbox-label">
          <input
            type="checkbox"
            checked={termsAccepted}
            onChange={(e) => onTermsChange(e.target.checked)}
            data-testid="checkbox-terms"
          />
          I accept the terms and conditions
        </label>
        {errors.termsAccepted && (
          <span className="error" data-testid="error-termsAccepted">
            {errors.termsAccepted[0]}
          </span>
        )}
      </div>
    </div>
  );
}
