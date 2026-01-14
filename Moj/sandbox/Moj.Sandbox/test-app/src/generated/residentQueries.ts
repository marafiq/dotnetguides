import { useQuery, queryOptions, useMutation } from '@tanstack/react-query';

export interface Resident {
  id?: string;
  firstName?: string;
  middleName?: string;
  lastName?: string;
  preferredName?: string;
  dateOfBirth: string;
  gender?: string;
  photoUrl?: string;
  status: 'active' | 'temporary' | 'onLeave' | 'hospitalized' | 'discharged' | 'deceased';
  careLevel: 'independent' | 'assistedLiving' | 'memoryCare' | 'skilledNursing' | 'hospice';
  admissionDate: string;
  dischargeDate?: string | null;
  room?: Room;
  emergencyContacts?: EmergencyContact[];
  personalContact?: ContactInfo;
  healthRecord?: HealthRecord;
  carePlan?: CarePlan;
  assignedStaff?: StaffMember[];
  primaryCaregiverId?: string;
  enrolledActivityIds?: string[];
  activeAlerts?: ResidentAlert[];
  interests?: string[];
  languages?: string[];
  religiousPreference?: string;
  insuranceProvider?: string;
  insurancePolicyNumber?: string;
  medicaidId?: string;
  medicareId?: string;
  notes?: string;
  createdAt: string;
  updatedAt: string;
}

export type ResidentStatus = 'active' | 'temporary' | 'onLeave' | 'hospitalized' | 'discharged' | 'deceased';

export type CareLevel = 'independent' | 'assistedLiving' | 'memoryCare' | 'skilledNursing' | 'hospice';

export interface Room {
  id?: string;
  number?: string;
  floor: number;
  building?: string;
  type: 'studio' | 'oneBedroom' | 'twoBedroom' | 'suite' | 'shared' | 'privateMemoryCare';
  capacity: number;
  amenities?: string[];
  monthlyRate: number;
  isAvailable: boolean;
  currentResidentId?: string;
}

export type RoomType = 'studio' | 'oneBedroom' | 'twoBedroom' | 'suite' | 'shared' | 'privateMemoryCare';

export interface EmergencyContact {
  id?: string;
  name?: string;
  relationship?: string;
  contact?: ContactInfo;
  address?: Address;
  isPrimaryContact: boolean;
  hasPowerOfAttorney: boolean;
  isHealthcareProxy: boolean;
}

export interface ContactInfo {
  phone?: string;
  alternatePhone?: string;
  email?: string;
  preferredContactMethod?: string;
}

export interface Address {
  street?: string;
  unit?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  country?: string;
}

export interface HealthRecord {
  id?: string;
  residentId?: string;
  conditions?: HealthCondition[];
  allergies?: Allergy[];
  medications?: Medication[];
  vitalHistory?: VitalSigns[];
  latestVitals?: VitalSigns;
  bloodType?: string;
  primaryPhysician?: string;
  physicianPhone?: string;
  lastPhysicianVisit?: string | null;
  nextPhysicianVisit?: string | null;
  immunizations?: string[];
  advanceDirectives?: string;
  hasDnr: boolean;
}

export interface HealthCondition {
  id?: string;
  name?: string;
  icdCode?: string;
  diagnosedDate: string;
  diagnosedBy?: string;
  notes?: string;
  isActive: boolean;
}

export interface Allergy {
  id?: string;
  allergen?: string;
  type?: string;
  severity?: string;
  reaction?: string;
  identifiedDate?: string | null;
}

export interface Medication {
  id?: string;
  name?: string;
  genericName?: string;
  dosage?: string;
  route?: string;
  frequency: 'asNeeded' | 'daily' | 'twiceDaily' | 'threeTimesDaily' | 'fourTimesDaily' | 'weekly' | 'monthly';
  scheduleTimes?: string[];
  prescriber?: string;
  pharmacy?: string;
  startDate: string;
  endDate?: string | null;
  instructions?: string;
  sideEffects?: string[];
  interactions?: string[];
  isActive: boolean;
}

export type MedicationFrequency = 'asNeeded' | 'daily' | 'twiceDaily' | 'threeTimesDaily' | 'fourTimesDaily' | 'weekly' | 'monthly';

export interface VitalSigns {
  id?: string;
  recordedAt: string;
  recordedBy?: string;
  bloodPressureSystolic?: number | null;
  bloodPressureDiastolic?: number | null;
  heartRate?: number | null;
  temperature?: number | null;
  respiratoryRate?: number | null;
  oxygenSaturation?: number | null;
  weight?: number | null;
  notes?: string;
}

export interface CarePlan {
  id?: string;
  createdDate: string;
  lastReviewDate: string;
  nextReviewDate: string;
  primaryCaregiverId?: string;
  goals?: CarePlanGoal[];
  specialInstructions?: string;
  dailyRoutine?: string[];
  mobilityLevel: 'independent' | 'caneOrWalker' | 'wheelchair' | 'bedridden' | 'requiresAssistance';
  dietType: 'regular' | 'diabetic' | 'lowSodium' | 'pureed' | 'mechanical' | 'glutenFree' | 'vegetarian' | 'kosher';
  dietaryRestrictions?: string[];
  requiresAssistanceWithAdls: boolean;
  adlAssistanceNeeded?: string[];
}

export interface CarePlanGoal {
  id?: string;
  description?: string;
  category?: string;
  targetDate: string;
  status?: string;
  progressPercent: number;
  interventions?: string[];
  outcome?: string;
}

export type MobilityLevel = 'independent' | 'caneOrWalker' | 'wheelchair' | 'bedridden' | 'requiresAssistance';

export type DietType = 'regular' | 'diabetic' | 'lowSodium' | 'pureed' | 'mechanical' | 'glutenFree' | 'vegetarian' | 'kosher';

export interface StaffMember {
  id?: string;
  firstName?: string;
  lastName?: string;
  role: 'caregiver' | 'nurse' | 'physician' | 'activityDirector' | 'administrator' | 'dietitian' | 'physicalTherapist' | 'socialWorker';
  credentials?: string;
  contact?: ContactInfo;
  assignedResidentIds?: string[];
  photoUrl?: string;
  isOnDuty: boolean;
}

export type StaffRole = 'caregiver' | 'nurse' | 'physician' | 'activityDirector' | 'administrator' | 'dietitian' | 'physicalTherapist' | 'socialWorker';

export interface ResidentAlert {
  id?: string;
  residentId?: string;
  type?: string;
  severity: 'info' | 'low' | 'medium' | 'high' | 'critical';
  message?: string;
  createdAt: string;
  acknowledgedAt?: string | null;
  acknowledgedBy?: string;
  isResolved: boolean;
  resolution?: string;
}

export type AlertSeverity = 'info' | 'low' | 'medium' | 'high' | 'critical';

export interface FacilityActivity {
  id?: string;
  name?: string;
  description?: string;
  type: 'social' | 'physical' | 'cognitive' | 'creative' | 'spiritual' | 'medical' | 'dining' | 'therapy';
  startTime: string;
  endTime: string;
  location?: string;
  leadStaffId?: string;
  maxParticipants: number;
  registeredResidentIds?: string[];
  isRecurring: boolean;
  recurrencePattern?: string;
}

export type ActivityType = 'social' | 'physical' | 'cognitive' | 'creative' | 'spiritual' | 'medical' | 'dining' | 'therapy';

export interface List`1 {
  capacity: number;
  readonly count: number;
  item?: Resident;
}

export const residentsOptions = queryOptions({ queryKey: ['residents', 'list'], queryFn: () => fetch('/api/residents').then(response => response.json()), staleTime: 30000 });

export const useResidents = () => useQuery(residentsOptions);

export const residentOptions = queryOptions({ queryKey: ['residents', 'detail'], queryFn: () => fetch('/api/residents/{id}').then(response => response.json()), staleTime: 60000 });

export const useResident = () => useQuery(residentOptions);

export interface List`1 {
  capacity: number;
  readonly count: number;
  item?: Room;
}

export const roomsOptions = queryOptions({ queryKey: ['rooms', 'list'], queryFn: () => fetch('/api/rooms').then(response => response.json()), staleTime: 300000 });

export const useRooms = () => useQuery(roomsOptions);

export interface List`1 {
  capacity: number;
  readonly count: number;
  item?: Room;
}

export const availableRoomsOptions = queryOptions({ queryKey: ['rooms', 'available'], queryFn: () => fetch('/api/rooms?available=true').then(response => response.json()), staleTime: 60000 });

export const useAvailableRooms = () => useQuery(availableRoomsOptions);

export interface List`1 {
  capacity: number;
  readonly count: number;
  item?: StaffMember;
}

export const staffOptions = queryOptions({ queryKey: ['staff', 'list'], queryFn: () => fetch('/api/staff').then(response => response.json()), staleTime: 120000 });

export const useStaff = () => useQuery(staffOptions);

export interface List`1 {
  capacity: number;
  readonly count: number;
  item?: StaffMember;
}

export const onDutyStaffOptions = queryOptions({
queryKey: ['staff', 'on-duty'],
queryFn: () => fetch('/api/staff?onDuty=true').then(response => response.json()),
staleTime: 30000,
refetchInterval: 60000
});

export const useOnDutyStaff = () => useQuery(onDutyStaffOptions);

export interface List`1 {
  capacity: number;
  readonly count: number;
  item?: FacilityActivity;
}

export const activitiesOptions = queryOptions({ queryKey: ['activities', 'list'], queryFn: () => fetch('/api/activities').then(response => response.json()), staleTime: 120000 });

export const useActivities = () => useQuery(activitiesOptions);

export interface List`1 {
  capacity: number;
  readonly count: number;
  item?: FacilityActivity;
}

export const todaysActivitiesOptions = queryOptions({ queryKey: ['activities', 'today'], queryFn: () => fetch('/api/activities/today').then(response => response.json()), staleTime: 60000 });

export const useTodaysActivities = () => useQuery(todaysActivitiesOptions);

export interface List`1 {
  capacity: number;
  readonly count: number;
  item?: ResidentAlert;
}

export const alertsOptions = queryOptions({
queryKey: ['alerts', 'unacknowledged'],
queryFn: () => fetch('/api/alerts?acknowledged=false').then(response => response.json()),
staleTime: 15000,
refetchInterval: 30000
});

export const useAlerts = () => useQuery(alertsOptions);

export const residentHealthOptions = queryOptions({ queryKey: ['residents', 'health'], queryFn: () => fetch('/api/residents/{id}/health').then(response => response.json()), staleTime: 60000 });

export const useResidentHealth = () => useQuery(residentHealthOptions);

export const residentCarePlanOptions = queryOptions({ queryKey: ['residents', 'careplan'], queryFn: () => fetch('/api/residents/{id}/careplan').then(response => response.json()), staleTime: 120000 });

export const useResidentCarePlan = () => useQuery(residentCarePlanOptions);

export const createResidentOptions = { mutationKey: ['residents', 'create'], mutationFn: (variables: Resident) => fetch('/api/residents', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(variables) }).then(response => response.json()) };

export const useCreateResident = () => useMutation(createResidentOptions);

export const updateResidentOptions = { mutationKey: ['residents', 'update'], mutationFn: (variables: Resident) => fetch('/api/residents/{id}', { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(variables) }).then(response => response.json()) };

export const useUpdateResident = () => useMutation(updateResidentOptions);

export const acknowledgeAlertOptions = { mutationKey: ['alerts', 'acknowledge'], mutationFn: (variables: ResidentAlert) => fetch('/api/alerts/{id}/acknowledge', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(variables) }).then(response => response.json()) };

export const useAcknowledgeAlert = () => useMutation(acknowledgeAlertOptions);