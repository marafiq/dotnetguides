import { useState } from 'react';
import type { ImpulseComponentProps } from '@impulse/react';

interface WizardField {
  name: string;
  label: string;
  type: 'text' | 'date' | 'textarea' | 'select' | 'checkbox' | 'checkboxGroup';
  required?: boolean;
  options?: { value: string; label: string }[];
}

interface WizardStep {
  id: string;
  title: string;
  description: string;
  fields: WizardField[];
}

interface AdmissionWizardData {
  currentStep: number;
  totalSteps: number;
  step: WizardStep;
  formData: Record<string, unknown>;
  submitUrl: string;
  backUrl?: string;
}

export function AdmissionWizard({
  data,
  navigate,
  submit,
  errors,
  isSubmitting,
}: ImpulseComponentProps<AdmissionWizardData>) {
  const [formValues, setFormValues] = useState<Record<string, unknown>>(data.formData || {});

  const handleFieldChange = (name: string, value: unknown) => {
    setFormValues((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async () => {
    try {
      const result = await submit<Record<string, unknown>, { redirect?: string }>(
        data.submitUrl,
        formValues
      );
      if (result?.redirect) {
        navigate(result.redirect);
      }
    } catch {
      // Validation errors are handled by ImpulseHost
    }
  };

  const handleBack = () => {
    if (data.backUrl) {
      navigate(data.backUrl);
    }
  };

  const renderField = (field: WizardField) => {
    const fieldErrors = errors[field.name] || [];
    const value = formValues[field.name] ?? '';

    switch (field.type) {
      case 'text':
        return (
          <div key={field.name} className="field">
            <label>{field.label} {field.required && '*'}</label>
            <input
              type="text"
              value={String(value)}
              onChange={(e) => handleFieldChange(field.name, e.target.value)}
              data-testid={`input-${field.name}`}
            />
            {fieldErrors.length > 0 && (
              <span className="error" data-testid={`error-${field.name}`}>
                {fieldErrors[0]}
              </span>
            )}
          </div>
        );

      case 'date':
        return (
          <div key={field.name} className="field">
            <label>{field.label} {field.required && '*'}</label>
            <input
              type="date"
              value={String(value)}
              onChange={(e) => handleFieldChange(field.name, e.target.value)}
              data-testid={`input-${field.name}`}
            />
            {fieldErrors.length > 0 && (
              <span className="error" data-testid={`error-${field.name}`}>
                {fieldErrors[0]}
              </span>
            )}
          </div>
        );

      case 'textarea':
        return (
          <div key={field.name} className="field">
            <label>{field.label}</label>
            <textarea
              value={String(value)}
              onChange={(e) => handleFieldChange(field.name, e.target.value)}
              data-testid={`input-${field.name}`}
            />
          </div>
        );

      case 'select':
        return (
          <div key={field.name} className="field">
            <label>{field.label} {field.required && '*'}</label>
            <select
              value={String(value)}
              onChange={(e) => handleFieldChange(field.name, e.target.value)}
              data-testid={`input-${field.name}`}
            >
              {field.options?.map((opt) => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </select>
          </div>
        );

      case 'checkbox':
        return (
          <div key={field.name} className="field checkbox">
            <label>
              <input
                type="checkbox"
                checked={Boolean(value)}
                onChange={(e) => handleFieldChange(field.name, e.target.checked)}
                data-testid={`checkbox-${field.name}`}
              />
              {field.label}
            </label>
            {fieldErrors.length > 0 && (
              <span className="error" data-testid={`error-${field.name}`}>
                {fieldErrors[0]}
              </span>
            )}
          </div>
        );

      case 'checkboxGroup':
        const selectedValues = (value as string[]) || [];
        return (
          <div key={field.name} className="field">
            <label>{field.label}</label>
            <div className="checkbox-group">
              {field.options?.map((opt) => (
                <label key={opt.value}>
                  <input
                    type="checkbox"
                    checked={selectedValues.includes(opt.value)}
                    onChange={(e) => {
                      const newValues = e.target.checked
                        ? [...selectedValues, opt.value]
                        : selectedValues.filter((v) => v !== opt.value);
                      handleFieldChange(field.name, newValues);
                    }}
                    data-testid={`checkbox-${opt.value}`}
                  />
                  {opt.label}
                </label>
              ))}
            </div>
          </div>
        );

      default:
        return null;
    }
  };

  return (
    <div className="card" data-testid="admission-wizard">
      <div className="wizard-progress" data-testid={data.step.id}>
        {Array.from({ length: data.totalSteps }, (_, i) => (
          <div
            key={i}
            className={`step ${i + 1 <= data.currentStep ? 'active' : ''}`}
          >
            {i + 1}
          </div>
        ))}
      </div>

      <h2>{data.step.title}</h2>
      <p className="step-description">{data.step.description}</p>

      {/* Show summary on review step */}
      {data.step.id === 'review-step' && data.formData && (() => {
        const fd = data.formData as Record<string, string>;
        return (
          <div className="form-summary">
            {fd.firstName && fd.lastName && (
              <p><strong>Name:</strong> {fd.firstName} {fd.lastName}</p>
            )}
            {fd.dateOfBirth && (
              <p><strong>Date of Birth:</strong> {fd.dateOfBirth}</p>
            )}
            {fd.careLevel && (
              <p><strong>Care Level:</strong> {fd.careLevel}</p>
            )}
          </div>
        );
      })()}

      <form onSubmit={(e) => { e.preventDefault(); handleSubmit(); }}>
        {data.step.fields.map(renderField)}

        <div className="wizard-actions">
          {data.backUrl && (
            <button
              type="button"
              onClick={handleBack}
              disabled={isSubmitting}
              data-testid="back-button"
            >
              Back
            </button>
          )}
          <button
            type="submit"
            className="btn-primary"
            disabled={isSubmitting}
            data-testid="next-button"
          >
            {data.currentStep === data.totalSteps ? 'Complete' : 'Next'}
          </button>
        </div>
      </form>
    </div>
  );
}
