-- check_rls_coverage.sql
-- Teste de CI obrigatório (ADR-0007, seção Consequências e Riscos).
-- Falha (retorna linhas) se existir tabela com coluna `tenant_id`
-- cujo Row-Level Security não esteja habilitado.
--
-- Uso sugerido no pipeline:
--   psql "$DATABASE_URL" -v ON_ERROR_STOP=1 -f tests/rls/check_rls_coverage.sql \
--     | grep -q 'sem RLS' && exit 1 || exit 0
-- (ou adaptar para o formato de CI escolhido quando a linguagem de
-- backend for decidida — este teste é independente de linguagem).

SELECT
    c.relname AS tabela,
    'sem RLS' AS problema
FROM pg_class c
JOIN pg_namespace n ON n.oid = c.relnamespace
WHERE c.relkind = 'r'
  AND n.nspname = 'public'
  AND c.relname <> 'tenants'  -- exceção documentada (ADR-0007 / ADR-0009)
  AND EXISTS (
      SELECT 1 FROM information_schema.columns col
      WHERE col.table_name = c.relname
        AND col.column_name = 'tenant_id'
  )
  AND NOT c.relrowsecurity;

-- Nenhuma linha retornada = passa. Qualquer linha retornada = falha o
-- build (mesma disciplina do teste de invariante de moeda única em
-- migrations/0001_init.sql — a garantia vive no banco, não apenas na
-- convenção documentada).
