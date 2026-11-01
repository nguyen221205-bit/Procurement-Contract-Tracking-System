import { ENUMS } from './enums.generated.js';

export function enumValue(domain, value) {
  const members = ENUMS[domain];
  if (!members || value === null || value === undefined || value === '') {
    throw new Error(`Thiếu giá trị enum ${domain}.`);
  }
  const numeric = typeof value === 'number' ? value : members[value] ?? (/^\d+$/.test(value) ? Number(value) : NaN);
  if (!Object.values(members).includes(numeric)) throw new Error(`Giá trị ${domain} không hợp lệ: ${value}`);
  return numeric;
}

export function tryEnumValue(domain, value) {
  try { return enumValue(domain, value); } catch { return null; }
}

// Numeric wire values, with legacy enum names accepted only at this boundary.
export function encodeRequestEnums(config) {
  const url = config.url || '';
  const data = config.data;
  const params = { ...config.params };
  const hasValue = (value) => value !== undefined && value !== null && value !== '';
  if (/^\/bid-packages(?:\/|$)/.test(url)) {
    if (hasValue(params.type)) params.type = enumValue('BidPackageType', params.type);
    if (hasValue(params.status)) params.status = enumValue('BidPackageStatus', params.status);
  }
  if (/^\/contracts(?:\/|$)/.test(url) && hasValue(params.status))
    params.status = enumValue('ContractStatus', params.status);
  if (data && typeof data === 'object' && !(typeof FormData !== 'undefined' && data instanceof FormData)) {
    const result = { ...data };
    if ('newStatus' in result && /^\/bid-packages\//.test(url)) result.newStatus = enumValue('BidPackageStatus', result.newStatus);
    if ('newStatus' in result && /^\/contracts\//.test(url)) result.newStatus = enumValue('ContractStatus', result.newStatus);
    if ('type' in result && url === '/bid-packages') result.type = enumValue('BidPackageType', result.type);
    if ('type' in result && /^\/contracts\/\d+\/addenda$/.test(url)) result.type = enumValue('AddendumType', result.type);
    if ('status' in result && /\/guarantees\/\d+\/status$/.test(url)) result.status = enumValue('GuaranteeStatus', result.status);
    config.data = result;
  }
  config.params = params;
  return config;
}
