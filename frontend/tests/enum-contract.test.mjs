import test from 'node:test';
import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { ENUMS } from '../src/api/enums.generated.js';
import { enumValue, tryEnumValue, encodeRequestEnums } from '../src/api/enumContract.js';
import { PACKAGE_STATUS_CONFIG } from '../src/utils/constants.js';

test('generated enums match BE source including Cancelled and Success', () => {
  execFileSync(process.execPath, ['scripts/generate-enums.mjs', '--check']);
  assert.equal(ENUMS.BidPackageStatus.Cancelled, 5);
  assert.equal(ENUMS.NotificationType.Success, 5);
  for (const [code, id] of Object.entries(ENUMS.BidPackageStatus)) {
    assert.ok(PACKAGE_STATUS_CONFIG[code]); assert.ok(PACKAGE_STATUS_CONFIG[id]);
  }
});
test('wire request uses enum IDs and preserves zero filters', () => {
  const input = { url: '/bid-packages', data: { type: 'Goods', name: 'Fixture' }, params: { status: 'Open', type: 'Goods' } };
  const result = encodeRequestEnums({ ...input });
  assert.equal(result.data.type, 0);
  assert.deepEqual(result.params, { status: 0, type: 0 });
  assert.equal(input.data.type, 'Goods');
  assert.equal(encodeRequestEnums({ url: '/bid-packages/1/status', data: { newStatus: 'Closed' } }).data.newStatus, 1);
  assert.equal(encodeRequestEnums({ url: '/contracts/1/status', data: { newStatus: 'Active' } }).data.newStatus, 1);
  assert.equal(encodeRequestEnums({ url: '/contracts/1/addenda', data: { type: 'ScopeChange' } }).data.type, 2);
});
test('unknown enum is rejected, names and numeric REST/realtime values normalize equally', () => {
  assert.equal(enumValue('NotificationType', 'Warning'), 1);
  assert.equal(enumValue('NotificationType', 1), 1);
  assert.equal(enumValue('NotificationType', 'Success'), 5);
  assert.equal(tryEnumValue('NotificationType', 'Unknown'), null);
  for (const value of [999, null, undefined, '', 'Đang mở thầu'])
    assert.throws(() => enumValue('BidPackageStatus', value));
});
test('string domain statuses and role codes are not converted into invented IDs', () => {
  const roles = { roles: ['Admin'] };
  assert.deepEqual(encodeRequestEnums({ url: '/users/1/roles', data: roles }).data, roles);
  assert.equal(encodeRequestEnums({ url: '/evaluator-proposals', params: { status: 'Pending' } }).params.status, 'Pending');
});
