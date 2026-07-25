package com.example.test;

import androidx.room.Entity;
import androidx.room.ForeignKey;
import androidx.room.PrimaryKey;


@Entity(tableName = "enemy")
public class Enemy {
    // 기본키
    @PrimaryKey(autoGenerate = true)
    private int id;
    // 맵 번호
    private int mapNumber;
    // 등장시간
    private int time;
    // 몬스터 이름 Foreign Key
    private String name;

    public int getId() {
        return id;
    }

    public void setId(int id) {
        this.id = id;
    }

    public int getMapNumber() {
        return mapNumber;
    }

    public void setMapNumber(int mapNumber) {
        this.mapNumber = mapNumber;
    }

    public int getTime() {
        return time;
    }

    public void setTime(int time) {
        this.time = time;
    }

    public String getName() {
        return name;
    }

    public void setName(String name) {
        this.name = name;
    }

    public Enemy(int mapNumber, int time, String name) {
        this.mapNumber = mapNumber;
        this.time = time;
        this.name = name;
    }
}
