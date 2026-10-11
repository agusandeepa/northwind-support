import { describe, expect, it } from 'vitest';
import { validateEscalation, whyNotEscalatable } from './escalationRules';

describe('validateEscalation', () => {
  it('accepts a reason of exactly 5 and exactly 500 characters', () => {
    expect(validateEscalation('abcde', 'Alex').reason).toBeUndefined();
    expect(validateEscalation('a'.repeat(500), 'Alex').reason).toBeUndefined();
  });

  it('rejects a reason of 4 or 501 characters', () => {
    expect(validateEscalation('abcd', 'Alex').reason).toBeDefined();
    expect(validateEscalation('a'.repeat(501), 'Alex').reason).toBeDefined();
  });

  it('measures the reason after trimming, as the API does', () => {
    expect(validateEscalation('   abcd   ', 'Alex').reason).toBeDefined();
    expect(validateEscalation('   abcde   ', 'Alex').reason).toBeUndefined();
  });

  it('needs a name of at most 100 characters', () => {
    expect(validateEscalation('A good reason', '   ').escalatedBy).toBeDefined();
    expect(validateEscalation('A good reason', 'a'.repeat(101)).escalatedBy).toBeDefined();
    expect(validateEscalation('A good reason', 'a'.repeat(100)).escalatedBy).toBeUndefined();
  });

  it('finds nothing wrong with good input', () => {
    expect(validateEscalation('A good reason', 'Alex')).toEqual({});
  });
});

describe('whyNotEscalatable', () => {
  it('allows an open ticket below Critical', () => {
    expect(whyNotEscalatable({ status: 'Open', priority: 'High' })).toBeUndefined();
  });

  it('refuses a Critical ticket and says so', () => {
    expect(whyNotEscalatable({ status: 'Open', priority: 'Critical' })).toMatch(/critical/i);
  });

  it.each(['Resolved', 'Closed'] as const)('refuses a %s ticket and names the status', (status) => {
    expect(whyNotEscalatable({ status, priority: 'Low' })).toMatch(new RegExp(status, 'i'));
  });
});
