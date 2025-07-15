-- 添加更多学生信息供注册测试
INSERT INTO students (student_id, name, department) VALUES ('2352495', '张竹和', '计算机科学与技术学院');
INSERT INTO students (student_id, name, department) VALUES ('2353018', '钱宝强', '计算机科学与技术学院');
INSERT INTO students (student_id, name, department) VALUES ('2351427', '缪语欣', '计算机科学与技术学院');
INSERT INTO students (student_id, name, department) VALUES ('2354177', '陈雷诗语', '计算机科学与技术学院');
INSERT INTO students (student_id, name, department) VALUES ('2352755', '刘奕含', '计算机科学与技术学院');
INSERT INTO students (student_id, name, department) VALUES ('2352491', '郭艺', '计算机科学与技术学院');
INSERT INTO students (student_id, name, department) VALUES ('2351284', '李思远', '计算机科学与技术学院');
INSERT INTO students (student_id, name, department) VALUES ('2354269', '刘笑云', '计算机科学与技术学院');
INSERT INTO students (student_id, name, department) VALUES ('2352749', '戴湘宁', '计算机科学与技术学院');
INSERT INTO students (student_id, name, department) VALUES ('2351588', '谭鹏翀', '计算机科学与技术学院');
COMMIT;

-- 查看所有学生信息
SELECT * FROM students ORDER BY student_id;