import { useState } from 'react';
import { useImpulse, useImpulsePage, isValidationError } from '@impulse/react';
import { useMutation } from '@tanstack/react-query';
import type {
  AdmissionWizardResponse,
  BasicInfoRequest,
  MedicalHistoryRequest,
  CarePreferencesRequest,
  EmergencyContactsRequest,
  ValidateStepResponse,
  CompleteAdmissionResponse,
  CareLevel,
  DietaryRequirement,
} from '../../generated/types';
import { RoutePaths } from '../../generated/routePaths';
import { BasicInfoStep } from './steps/BasicInfoStep';
import { MedicalHistoryStep } from './steps/MedicalHistoryStep';
import { CarePreferencesStep } from './steps/CarePreferencesStep';
import { EmergencyContactsStep } from './steps/EmergencyContactsStep';
import { ReviewStep } from './steps/ReviewStep';

interface AdmissionWizardProps {
  onComplete: () => void;
}

export function AdmissionWizard({ onComplete }: AdmissionWizardProps) {
  const { impulseMutate } = useImpulse();
  const { data: config, isLoading } = useImpulsePage<AdmissionWizardResponse>(
    RoutePaths.GetAdmissionWizard
  );

  const [currentStep, setCurrentStep] = useState(0);
  const [errors, setErrors] = useState<Record<string, string[]>>({});

  const [basicInfo, setBasicInfo] = useState<BasicInfoRequest>({
    firstName: '',
    lastName: '',
    dateOfBirth: '',
    roomPreference: null,
  });

  const [medicalHistory, setMedicalHistory] = useState<MedicalHistoryRequest>({
    conditions: [],
    medications: [],
    allergies: null,
    primaryCarePhysician: null,
  });

  const [carePreferences, setCarePreferences] = useState<CarePreferencesRequest>({
    careLevel: 'Independent' as CareLevel,
    dietaryRequirements: [] as DietaryRequirement[],
    specialInstructions: null,
    requiresNightChecks: false,
  });

  const [emergencyContacts, setEmergencyContacts] = useState<EmergencyContactsRequest>({
    primaryContactName: '',
    primaryContactPhone: '',
    primaryContactRelationship: '',
    secondaryContactName: null,
    secondaryContactPhone: null,
  });

  const [consentGiven, setConsentGiven] = useState(false);
  const [termsAccepted, setTermsAccepted] = useState(false);

  const validateMutation = useMutation({
    mutationFn: async (step: string) => {
      let data: unknown;
      let url: string;
      switch (step) {
        case 'basic-info':
          data = basicInfo;
          url = RoutePaths.ValidateBasicInfo;
          break;
        case 'medical-history':
          data = medicalHistory;
          url = RoutePaths.ValidateMedicalHistory;
          break;
        case 'care-preferences':
          data = carePreferences;
          url = RoutePaths.ValidateCarePreferences;
          break;
        case 'emergency-contacts':
          data = emergencyContacts;
          url = RoutePaths.ValidateEmergencyContacts;
          break;
        default:
          return { isValid: true, errors: null };
      }
      return impulseMutate<unknown, ValidateStepResponse>(url, data, 'POST');
    },
  });

  const completeMutation = useMutation({
    mutationFn: async () => {
      return impulseMutate<unknown, CompleteAdmissionResponse>(
        RoutePaths.CompleteAdmission,
        {
          basicInfo,
          medicalHistory,
          carePreferences,
          emergencyContacts,
          consentGiven,
          termsAccepted,
        },
        'POST'
      );
    },
    onSuccess: () => {
      onComplete();
    },
    onError: (error) => {
      if (isValidationError(error)) {
        setErrors(error.errors);
      }
    },
  });

  const handleNext = async () => {
    if (!config) return;

    const stepId = config.steps[currentStep].id;

    if (stepId === 'review') {
      await completeMutation.mutateAsync();
      return;
    }

    const result = await validateMutation.mutateAsync(stepId);

    if (result.isValid) {
      setErrors({});
      setCurrentStep((prev) => Math.min(prev + 1, config.steps.length - 1));
    } else if (result.errors) {
      setErrors(result.errors);
    }
  };

  const handleBack = () => {
    setCurrentStep((prev) => Math.max(prev - 1, 0));
    setErrors({});
  };

  if (isLoading || !config) {
    return <div className="loading" data-testid="wizard-loading">Loading wizard...</div>;
  }

  const currentStepConfig = config.steps[currentStep];

  return (
    <div className="wizard" data-testid="admission-wizard">
      <div className="wizard-progress" data-testid="wizard-progress">
        {config.steps.map((step, index) => (
          <div
            key={step.id}
            className={`wizard-step ${index === currentStep ? 'active' : ''} ${
              index < currentStep ? 'completed' : ''
            }`}
            data-testid={`step-indicator-${step.id}`}
          >
            <span className="step-number">{index + 1}</span>
            <span className="step-title">{step.title}</span>
          </div>
        ))}
      </div>

      <div className="wizard-content" data-testid="wizard-content">
        <h2>{currentStepConfig.title}</h2>
        <p className="step-description">{currentStepConfig.description}</p>

        {currentStepConfig.id === 'basic-info' && (
          <BasicInfoStep data={basicInfo} onChange={setBasicInfo} errors={errors} />
        )}

        {currentStepConfig.id === 'medical-history' && (
          <MedicalHistoryStep data={medicalHistory} onChange={setMedicalHistory} />
        )}

        {currentStepConfig.id === 'care-preferences' && (
          <CarePreferencesStep
            data={carePreferences}
            onChange={setCarePreferences}
            careLevels={config.careLevels}
            dietaryOptions={config.dietaryRequirements}
          />
        )}

        {currentStepConfig.id === 'emergency-contacts' && (
          <EmergencyContactsStep
            data={emergencyContacts}
            onChange={setEmergencyContacts}
            errors={errors}
          />
        )}

        {currentStepConfig.id === 'review' && (
          <ReviewStep
            basicInfo={basicInfo}
            medicalHistory={medicalHistory}
            carePreferences={carePreferences}
            emergencyContacts={emergencyContacts}
            consentGiven={consentGiven}
            termsAccepted={termsAccepted}
            onConsentChange={setConsentGiven}
            onTermsChange={setTermsAccepted}
            errors={errors}
          />
        )}
      </div>

      <div className="wizard-actions" data-testid="wizard-actions">
        <button
          type="button"
          onClick={handleBack}
          disabled={currentStep === 0}
          data-testid="back-button"
        >
          Back
        </button>
        <button
          type="button"
          onClick={handleNext}
          disabled={validateMutation.isPending || completeMutation.isPending}
          data-testid="next-button"
        >
          {currentStep === config.steps.length - 1 ? 'Complete' : 'Next'}
        </button>
      </div>
    </div>
  );
}
