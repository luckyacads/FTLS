START TRANSACTION;
ALTER TABLE users ADD faculty_type character varying(20);

    UPDATE users
    SET faculty_type = 'Full-Time',
        max_units = 30
    WHERE role IN ('Faculty', 'Chairman');

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260818031911_AddFacultyTypeToUsers', '10.0.8');

COMMIT;

