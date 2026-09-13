type PaginationProps = {
  page: number;
  pageSize: number;
  totalCount: number;
  onPageChange: (page: number) => void;
  loading?: boolean;
};

export function Pagination({ page, pageSize, totalCount, onPageChange, loading = false }: PaginationProps) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  return (
    <div className="pagination">
      <button
        type="button"
        className="btn btn-ghost"
        disabled={loading || page <= 1}
        onClick={() => onPageChange(page - 1)}
      >
        Previous
      </button>
      <span className="muted small">
        Page {page} of {totalPages} · {totalCount} total
      </span>
      <button
        type="button"
        className="btn btn-ghost"
        disabled={loading || page >= totalPages}
        onClick={() => onPageChange(page + 1)}
      >
        Next
      </button>
    </div>
  );
}
