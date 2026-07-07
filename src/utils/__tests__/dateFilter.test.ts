import { hasDateRangeFilter, isTimestampInDateRange } from '../dateFilter';

describe('dateFilter', () => {
  it('returns true when no filter is set', () => {
    expect(isTimestampInDateRange(new Date('2024-03-15T12:00:00'), {})).toBe(true);
    expect(hasDateRangeFilter({})).toBe(false);
  });

  it('filters by start date inclusive', () => {
    const filter = { dateFrom: '2024-03-01' };
    expect(hasDateRangeFilter(filter)).toBe(true);
    expect(isTimestampInDateRange(new Date('2024-02-29T23:59:59'), filter)).toBe(false);
    expect(isTimestampInDateRange(new Date('2024-03-01T00:00:00'), filter)).toBe(true);
  });

  it('filters by end date inclusive through end of day', () => {
    const filter = { dateTo: '2024-03-31' };
    expect(isTimestampInDateRange(new Date('2024-03-31T23:30:00'), filter)).toBe(true);
    expect(isTimestampInDateRange(new Date('2024-04-01T00:00:00'), filter)).toBe(false);
  });

  it('filters within a closed range', () => {
    const filter = { dateFrom: '2024-06-01', dateTo: '2024-06-30' };
    expect(isTimestampInDateRange(new Date('2024-05-31T12:00:00'), filter)).toBe(false);
    expect(isTimestampInDateRange(new Date('2024-06-15T12:00:00'), filter)).toBe(true);
    expect(isTimestampInDateRange(new Date('2024-07-01T12:00:00'), filter)).toBe(false);
  });
});
