import test from 'node:test';
import assert from 'node:assert/strict';
import { manualScorePreview } from '../src/utils/scorePreview.js';

test('unsaved preview normalizes MaxScore rather than treating every criterion as /100', () => {
  assert.equal(manualScorePreview([{ id: 1, maxScore: 50, weight: 100 }], { 1: { score: 40 } }).total, 80);
});
test('manual preview does not fabricate automatic price points or a complete /100 total', () => {
  const result = manualScorePreview([
    { id: 1, maxScore: 50, weight: 70 },
    { id: 2, maxScore: 100, weight: 30, isPriceCriteria: true },
  ], { 1: { score: 40 } });
  assert.equal(result.total, 56);
  assert.equal(result.maxWeight, 70);
  assert.equal(result.contributions[2], undefined);
});
