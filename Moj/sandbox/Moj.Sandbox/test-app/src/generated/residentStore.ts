import { Store } from '@tanstack/store';

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

export interface ResidentFilters {
  statuses?: ('active' | 'temporary' | 'onLeave' | 'hospitalized' | 'discharged' | 'deceased')[];
  careLevels?: ('independent' | 'assistedLiving' | 'memoryCare' | 'skilledNursing' | 'hospice')[];
  building?: string;
  floor?: number | null;
  searchQuery?: string;
  assignedStaffId?: string;
  hasActiveAlerts?: boolean | null;
  admittedAfter?: string | null;
  admittedBefore?: string | null;
}

export interface ResidentUiState {
  sidebarOpen: boolean;
  activeTab?: string;
  viewMode?: string;
  sort?: ResidentSortConfig;
  pagination?: PaginationState;
  showAlertPanel: boolean;
  showActivityCalendar: boolean;
}

export interface ResidentSortConfig {
  field?: string;
  direction?: string;
}

export interface PaginationState {
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export type DayOfWeek = 'sunday' | 'monday' | 'tuesday' | 'wednesday' | 'thursday' | 'friday' | 'saturday';

export type DateTimeKind = 'unspecified' | 'utc' | 'local';

export type ResidentStatus = 'active' | 'temporary' | 'onLeave' | 'hospitalized' | 'discharged' | 'deceased';

export type CareLevel = 'independent' | 'assistedLiving' | 'memoryCare' | 'skilledNursing' | 'hospice';

export type RoomType = 'studio' | 'oneBedroom' | 'twoBedroom' | 'suite' | 'shared' | 'privateMemoryCare';

export type MedicationFrequency = 'asNeeded' | 'daily' | 'twiceDaily' | 'threeTimesDaily' | 'fourTimesDaily' | 'weekly' | 'monthly';

export type MobilityLevel = 'independent' | 'caneOrWalker' | 'wheelchair' | 'bedridden' | 'requiresAssistance';

export type DietType = 'regular' | 'diabetic' | 'lowSodium' | 'pureed' | 'mechanical' | 'glutenFree' | 'vegetarian' | 'kosher';

export type StaffRole = 'caregiver' | 'nurse' | 'physician' | 'activityDirector' | 'administrator' | 'dietitian' | 'physicalTherapist' | 'socialWorker';

export type AlertSeverity = 'info' | 'low' | 'medium' | 'high' | 'critical';

export type ActivityType = 'social' | 'physical' | 'cognitive' | 'creative' | 'spiritual' | 'medical' | 'dining' | 'therapy';

export interface ResidentAppState {
  residents?: Resident[];
  selectedResident?: Resident;
  rooms?: Room[];
  staff?: StaffMember[];
  activities?: FacilityActivity[];
  unacknowledgedAlerts?: ResidentAlert[];
  currentUser?: StaffMember;
  isLoading: boolean;
  error?: string;
  filters?: ResidentFilters;
  ui?: ResidentUiState;
}

export const residentStore: Store<ResidentAppState> = new Store({
residents: [],
selectedResident: null,
rooms: [],
staff: [],
activities: [],
unacknowledgedAlerts: [],
currentUser: null,
isLoading: false,
error: null,
filters: {
statuses: [],
careLevels: [],
building: null,
floor: null,
searchQuery: null,
assignedStaffId: null,
hasActiveAlerts: null,
admittedAfter: null,
admittedBefore: null
},
ui: {
sidebarOpen: true,
activeTab: 'overview',
viewMode: 'list',
sort: { field: 'lastName', direction: 'asc' },
pagination: {
page: 1,
pageSize: 25,
totalItems: 0,
totalPages: 0
},
showAlertPanel: false,
showActivityCalendar: false
}
});

export const setResidents = (payload: Resident[]) => residentStore.setState((state: ResidentAppState) => ({ ...state }));

export const addResident = (payload: Resident) => residentStore.setState((state: ResidentAppState) => ({ ...state }));

export const updateResident = (payload: Resident) => residentStore.setState((state: ResidentAppState) => ({ ...state }));

export const removeResident = (payload: string) => residentStore.setState((state: ResidentAppState) => ({ ...state }));

export const selectResident = (payload: Resident) => residentStore.setState((state: ResidentAppState) => ({ ...state }));

export const startLoading = () => residentStore.setState((state: ResidentAppState) => ({ ...state }));

export const setError = (payload: string) => residentStore.setState((state: ResidentAppState) => ({ ...state }));

export const clearError = () => residentStore.setState((state: ResidentAppState) => ({ ...state }));

export const setFilters = (payload: ResidentFilters) => residentStore.setState((state: ResidentAppState) => ({ ...state }));

export const setStatusFilter = (payload: ('active' | 'temporary' | 'onLeave' | 'hospitalized' | 'discharged' | 'deceased')[]) => residentStore.setState((state: ResidentAppState) => ({ ...state }));

export const setCareLevelFilter = (payload: ('independent' | 'assistedLiving' | 'memoryCare' | 'skilledNursing' | 'hospice')[]) => residentStore.setState((state: ResidentAppState) => ({ ...state }));

export const setSearchQuery = (payload: string) => residentStore.setState((state: ResidentAppState) => ({ ...state }));

export const clearFilters = () => residentStore.setState((state: ResidentAppState) => ({ ...state }));

export const toggleSidebar = () => residentStore.setState((state: ResidentAppState) => ({ ...state }));

export const setActiveTab = (payload: string) => residentStore.setState((state: ResidentAppState) => ({ ...state }));

export const setViewMode = (payload: string) => residentStore.setState((state: ResidentAppState) => ({ ...state }));

export const setPage = (payload: number) => residentStore.setState((state: ResidentAppState) => ({ ...state }));

export const toggleAlertPanel = () => residentStore.setState((state: ResidentAppState) => ({ ...state }));

export const toggleActivityCalendar = () => residentStore.setState((state: ResidentAppState) => ({ ...state }));

export const addAlert = (payload: ResidentAlert) => residentStore.setState((state: ResidentAppState) => ({ ...state }));

export const acknowledgeAlert = (payload: string) => residentStore.setState((state: ResidentAppState) => ({ ...state }));

export const setStaff = (payload: StaffMember[]) => residentStore.setState((state: ResidentAppState) => ({ ...state }));

export const setRooms = (payload: Room[]) => residentStore.setState((state: ResidentAppState) => ({ ...state }));

export const setActivities = (payload: FacilityActivity[]) => residentStore.setState((state: ResidentAppState) => ({ ...state }));

export const setCurrentUser = (payload: StaffMember) => residentStore.setState((state: ResidentAppState) => ({ ...state }));

export const totalResidents = (state: ResidentAppState): number => {
return undefined as any;
};

export const activeResidents = (state: ResidentAppState): Resident[] => {
return undefined as any;
};

export const criticalAlerts = (state: ResidentAppState): ResidentAlert[] => {
return undefined as any;
};

export const availableRooms = (state: ResidentAppState): Room[] => {
return undefined as any;
};

export const onDutyStaff = (state: ResidentAppState): StaffMember[] => {
return undefined as any;
};

export const hasUnacknowledgedAlerts = (state: ResidentAppState): boolean => {
return undefined as any;
};