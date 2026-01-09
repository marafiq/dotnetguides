import React, { useState } from 'react';
import {
  Card,
  Button,
  ButtonGroup,
  Heading,
  Text,
  TextField,
  Divider,
  Badge,
} from '@react-spectrum/s2';
import type { AddAssessmentRequest, AssessmentType } from '../../generated/types';
import { AddAssessmentRequestSchema } from '../../generated/validation';
import { useAddAssessmentMutation } from '../../generated/mutations';
import { useImpulse } from '../../client/shared/ImpulseProvider';
import { ZodError } from 'zod';

// ========================================
// Add Assessment Form
// Demonstrates:
// - Complex nested data (Record<string, string> for findings)
// - Dynamic array fields (recommendations)
// - Enum picker from generated types
// - Server-driven mutations
// ========================================

interface AddAssessmentFormProps {
  residentId: number;
  residentName: string;
  onCancel: () => void;
  onSuccess?: (assessmentId: number) => void;
}

// Predefined assessment categories for findings
const ASSESSMENT_CATEGORIES = [
  'Mobility',
  'Cognition',
  'Nutrition',
  'Skin Integrity',
  'Pain Level',
  'Sleep Quality',
  'Social Engagement',
  'ADL Independence',
  'Vital Signs',
  'Medication Adherence',
];

const ASSESSMENT_TYPES: { key: AssessmentType; label: string }[] = [
  { key: 'Initial', label: 'Initial Assessment' },
  { key: 'Quarterly', label: 'Quarterly Review' },
  { key: 'Annual', label: 'Annual Assessment' },
  { key: 'ChangeInCondition', label: 'Change in Condition' },
];

export function AddAssessmentForm({
  residentId,
  residentName,
  onCancel,
  onSuccess,
}: AddAssessmentFormProps) {
  const ctx = useImpulse();
  const mutation = useAddAssessmentMutation(ctx);

  // Form state
  const [assessmentType, setAssessmentType] = useState<AssessmentType>('Initial');
  const [assessmentDate, setAssessmentDate] = useState(new Date().toISOString().slice(0, 10));
  const [findings, setFindings] = useState<Record<string, string>>({});
  const [recommendations, setRecommendations] = useState<string[]>([]);
  const [newRecommendation, setNewRecommendation] = useState('');
  const [errors, setErrors] = useState<Record<string, string>>({});

  // Handle findings change
  const handleFindingChange = (category: string, value: string) => {
    setFindings(prev => ({
      ...prev,
      [category]: value,
    }));
  };

  // Clear a finding
  const clearFinding = (category: string) => {
    setFindings(prev => {
      const updated = { ...prev };
      delete updated[category];
      return updated;
    });
  };

  // Add recommendation
  const addRecommendation = () => {
    if (newRecommendation.trim()) {
      setRecommendations(prev => [...prev, newRecommendation.trim()]);
      setNewRecommendation('');
    }
  };

  // Remove recommendation
  const removeRecommendation = (index: number) => {
    setRecommendations(prev => prev.filter((_, i) => i !== index));
  };

  // Handle form submission
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrors({});

    const request: AddAssessmentRequest = {
      residentId,
      type: assessmentType,
      assessmentDate: new Date(assessmentDate).toISOString(),
      findings: Object.keys(findings).length > 0 ? findings : undefined,
      recommendations: recommendations.length > 0 ? recommendations : undefined,
    };

    try {
      AddAssessmentRequestSchema.parse(request);
      const result = await mutation.mutateAsync(request);
      onSuccess?.(result.assessmentId);
    } catch (err) {
      if (err instanceof ZodError) {
        const fieldErrors: Record<string, string> = {};
        err.errors.forEach(error => {
          fieldErrors[error.path.join('.')] = error.message;
        });
        setErrors(fieldErrors);
      } else if (err instanceof Error) {
        setErrors({ form: err.message });
      }
    }
  };

  const filledCategories = Object.keys(findings).length;
  const totalCategories = ASSESSMENT_CATEGORIES.length;

  return (
    <Card UNSAFE_className="impulse-card form-card">
      <form onSubmit={handleSubmit}>
        <div className="impulse-flex impulse-justify-between impulse-items-start">
          <div>
            <Heading level={2}>New Assessment</Heading>
            <Text UNSAFE_className="impulse-muted">
              Assessment for {residentName}
            </Text>
          </div>
          <Badge variant="informative" size="S">
            {filledCategories}/{totalCategories} categories
          </Badge>
        </div>
        <Divider />

        {errors.form && (
          <div className="form-error impulse-mb-4">
            <Text UNSAFE_className="impulse-refused">{errors.form}</Text>
          </div>
        )}

        {/* Assessment Type & Date */}
        <div className="form-section">
          <Heading level={3}>Assessment Details</Heading>
          <div className="form-row">
            <div className="impulse-flex impulse-flex-col impulse-gap-2">
              <Text UNSAFE_className="impulse-label">Assessment Type *</Text>
              <div className="impulse-flex impulse-gap-2 impulse-wrap">
                {ASSESSMENT_TYPES.map(type => (
                  <Button
                    key={type.key}
                    variant={assessmentType === type.key ? 'accent' : 'secondary'}
                    size="S"
                    onPress={() => setAssessmentType(type.key)}
                  >
                    {type.label}
                  </Button>
                ))}
              </div>
            </div>
            <TextField
              label="Assessment Date *"
              value={assessmentDate}
              onChange={setAssessmentDate}
              isRequired
            />
          </div>
        </div>

        <Divider />

        {/* Findings Section - Dynamic Record<string, string> */}
        <div className="form-section">
          <Heading level={3}>Clinical Findings</Heading>
          <Text UNSAFE_className="impulse-muted impulse-mb-4">
            Document findings for each applicable category.
          </Text>

          <div className="findings-grid">
            {ASSESSMENT_CATEGORIES.map((category) => (
              <Card key={category} UNSAFE_className="impulse-finding-card">
                <div className="impulse-flex impulse-justify-between impulse-items-center impulse-mb-2">
                  <Text UNSAFE_className="impulse-label-upper-sm">{category}</Text>
                  {findings[category] && (
                    <Button
                      variant="secondary"
                      size="S"
                      onPress={() => clearFinding(category)}
                    >
                      Clear
                    </Button>
                  )}
                </div>
                <TextField
                  aria-label={`${category} findings`}
                  value={findings[category] || ''}
                  onChange={(value) => handleFindingChange(category, value)}
                />
              </Card>
            ))}
          </div>
        </div>

        <Divider />

        {/* Recommendations Section - Dynamic Array */}
        <div className="form-section">
          <div className="impulse-flex impulse-justify-between impulse-items-center impulse-mb-4">
            <div>
              <Heading level={3} UNSAFE_style={{ margin: 0 }}>Recommendations</Heading>
              <Text UNSAFE_className="impulse-muted-sm">
                Add clinical recommendations based on findings
              </Text>
            </div>
            <Badge variant="neutral" size="S">
              {recommendations.length} added
            </Badge>
          </div>

          {/* Add new recommendation */}
          <div className="impulse-flex impulse-gap-2 impulse-mb-4">
            <div className="impulse-flex-1">
              <TextField
                aria-label="New recommendation"
                value={newRecommendation}
                onChange={setNewRecommendation}
              />
            </div>
            <Button variant="secondary" onPress={addRecommendation}>
              Add
            </Button>
          </div>

          {/* Recommendations list */}
          {recommendations.length > 0 ? (
            <div className="impulse-flex impulse-flex-col impulse-gap-2">
              {recommendations.map((rec, index) => (
                <Card key={index} UNSAFE_className="recommendation-item">
                  <div className="impulse-flex impulse-justify-between impulse-items-center">
                    <div className="impulse-flex impulse-items-center impulse-gap-2">
                      <Text UNSAFE_className="impulse-check">OK</Text>
                      <Text>{rec}</Text>
                    </div>
                    <Button
                      variant="secondary"
                      size="S"
                      onPress={() => removeRecommendation(index)}
                    >
                      Remove
                    </Button>
                  </div>
                </Card>
              ))}
            </div>
          ) : (
            <Text UNSAFE_className="impulse-muted">
              No recommendations added yet. Type above and click Add.
            </Text>
          )}
        </div>

        <Divider />

        {/* Form Actions */}
        <div className="form-actions">
          <ButtonGroup>
            <Button variant="secondary" onPress={onCancel}>
              Cancel
            </Button>
            <Button
              type="submit"
              variant="accent"
              isPending={mutation.isPending}
            >
              {mutation.isPending ? 'Saving...' : 'Save Assessment'}
            </Button>
          </ButtonGroup>
        </div>
      </form>
    </Card>
  );
}

// ========================================
// Add Assessment Modal Trigger
// ========================================

interface AddAssessmentButtonProps {
  residentId: number;
  residentName: string;
  onSuccess?: () => void;
}

export function AddAssessmentButton({ residentId, residentName, onSuccess }: AddAssessmentButtonProps) {
  const [isOpen, setIsOpen] = useState(false);

  if (isOpen) {
    return (
      <div className="modal-overlay">
        <div className="modal-content">
          <AddAssessmentForm
            residentId={residentId}
            residentName={residentName}
            onCancel={() => setIsOpen(false)}
            onSuccess={(id) => {
              setIsOpen(false);
              onSuccess?.();
            }}
          />
        </div>
      </div>
    );
  }

  return (
    <Button variant="accent" onPress={() => setIsOpen(true)}>
      + Add Assessment
    </Button>
  );
}
