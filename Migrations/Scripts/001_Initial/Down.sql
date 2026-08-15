DROP TABLE IF EXISTS public.resource_location;
DROP TABLE IF EXISTS public.locations;
DROP TRIGGER IF EXISTS trg_resources_updated_at ON public.resources;
DROP TABLE IF EXISTS public.resources;
DROP FUNCTION IF EXISTS public.set_updated_at();
