// Unsaved manual input only. Saved scores, price scores and rankings come from BE.
export function manualScorePreview(criteria, scores) {
  const totalWeight = criteria.reduce((sum, c) => sum + Number(c.weight), 0);
  const manual = criteria.filter((c) => !c.isPriceCriteria);
  const weighted = (c) => c.maxScore > 0 && totalWeight > 0
    ? Number(scores[c.id]?.score || 0) / c.maxScore * c.weight * 100 / totalWeight : 0;
  return {
    total: Math.round(manual.reduce((sum, c) => sum + weighted(c), 0) * 100) / 100,
    maxWeight: manual.reduce((sum, c) => sum + Number(c.weight), 0),
    contributions: Object.fromEntries(manual.map((c) => [c.id, weighted(c)])),
  };
}
