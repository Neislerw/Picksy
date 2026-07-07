export interface DateRangeFilter {
  dateFrom?: string;
  dateTo?: string;
}

export function hasDateRangeFilter(filter?: DateRangeFilter): boolean {
  return !!(filter?.dateFrom || filter?.dateTo);
}

function startOfDay(date: Date): Date {
  const d = new Date(date);
  d.setHours(0, 0, 0, 0);
  return d;
}

function endOfDay(date: Date): Date {
  const d = new Date(date);
  d.setHours(23, 59, 59, 999);
  return d;
}

function parseDateInput(value: string): Date | null {
  if (!value) return null;
  const parts = value.split('-').map((p) => parseInt(p, 10));
  if (parts.length !== 3 || parts.some((n) => Number.isNaN(n))) return null;
  const [year, month, day] = parts;
  const date = new Date(year, month - 1, day);
  return Number.isNaN(date.getTime()) ? null : date;
}

export function isTimestampInDateRange(timestamp: Date, filter?: DateRangeFilter): boolean {
  if (!hasDateRangeFilter(filter)) return true;
  const time = timestamp.getTime();
  if (filter?.dateFrom) {
    const from = parseDateInput(filter.dateFrom);
    if (from && time < startOfDay(from).getTime()) return false;
  }
  if (filter?.dateTo) {
    const to = parseDateInput(filter.dateTo);
    if (to && time > endOfDay(to).getTime()) return false;
  }
  return true;
}
