package com.example.test;

import androidx.room.Entity;
import androidx.room.Index;
import androidx.room.PrimaryKey;


@Entity(tableName = "player", indices = {@Index(value = {"name"}, unique = true)})
public class Player {
    // 기본키
    @PrimaryKey(autoGenerate = true)
    private int id;
    // 플레이어 이름
    private String name;
    // 돈
    private int money;
    // 맵 클리어 기록
    private String mapClear;

    public int getId() {
        return id;
    }

    public void setId(int id) {
        this.id = id;
    }

    public int getMoney() {
        return money;
    }

    public void setMoney(int money) {
        this.money = money;
    }

    public String getMapClear() {
        return mapClear;
    }

    public void setMapClear(String mapClear) {
        this.mapClear = mapClear;
    }

    public String getName() {
        return name;
    }

    public void setName(String name) {
        this.name = name;
    }

    public Player(String name, int money, String mapClear) {
        this.name = name;
        this.money = money;
        this.mapClear = mapClear;
    }
}
