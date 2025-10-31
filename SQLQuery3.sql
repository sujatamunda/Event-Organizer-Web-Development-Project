exec dbo.SP_GetVenuesByCity 'Bhubaneshwar','30-Apr-2025'


SELECT 
    CAST(VenueId AS VARCHAR) AS Value,
     VenueName + ' - ' + LTRIM(RIGHT(Location, LEN(Location) - CHARINDEX('-', Location))) AS Text from Venue WHERE VenueId NOT IN (
    SELECT distinct VenueId FROM Bookings WHERE convert(date,EventDate) = '30-Apr-2025' and VenueId is not null) and 
    LEFT(Location, CHARINDEX('-', Location + '-') - 1) = 'Bhubaneshwar'



select * from Venue where location like 'Bhubaneswar%'