CREATE EXTENSION IF NOT EXISTS postgis;
CREATE EXTENSION IF NOT EXISTS ltree;

CREATE TABLE public.resources(
    id SERIAL PRIMARY KEY,
    resource_branch LTREE NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    expires_in INTERVAL NOT NULL DEFAULT '0 seconds'::INTERVAL
);

CREATE FUNCTION public.set_updated_at() RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_resources_updated_at
    BEFORE UPDATE ON public.resources
    FOR EACH ROW EXECUTE FUNCTION public.set_updated_at();

CREATE TABLE public.locations(
    id SERIAL PRIMARY KEY,
    point GEOGRAPHY(Point, 4326) NOT NULL
);

CREATE TABLE public.resource_location(
    resource_id INT REFERENCES public.resources(Id),
    location_id INT REFERENCES public.locations(Id),
    PRIMARY KEY (resource_id,location_id)
);
CREATE INDEX idx_resource_location_location_id ON public.resource_location(location_id, resource_id);

CREATE INDEX idx_resources_branch ON public.resources USING GIST (resource_branch);
CREATE INDEX idx_locations_point ON public.locations USING GIST (point);
CREATE UNIQUE INDEX idx_locations_point_unique
    ON public.locations (ST_AsBinary(point));
