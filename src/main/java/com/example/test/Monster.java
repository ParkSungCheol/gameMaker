package com.example.test;

import android.os.CountDownTimer;
import android.widget.ImageView;
import android.widget.TextView;
import androidx.room.Entity;
import androidx.room.Ignore;
import androidx.room.Index;
import androidx.room.PrimaryKey;


@Entity(tableName = "monster", indices = {@Index(value = {"name"}, unique = true)})
public class Monster {
    // 기본키
    @PrimaryKey(autoGenerate = true)
    private int id;
    // 객체 이름, UNIQUE KEY
    private String name;
    // 객체 total HP
    private int total_hp;
    // 객체 공격력
    private int attack;
    // 객체 적군 인식 범위
    private int recognize_range;
    // 객체 이동속도(duration)
    private Long speed;
    // 객체 공격속도
    private Long attack_speed;
    // 타입상성
    private int type;
    // 공격스타일(단일 0, 범위 1, 원거리범위 2)
    private int attack_style;
    // 공격범위
    private int attack_range;
    // 넉백 확률
    private int percent;
    // cost
    private int cost;
    // 객체 이미지
    @Ignore
    private ImageView imageView;
    // 객체 공격 이벤트
    @Ignore
    private CountDownTimer countDownTimer;
    // 이동객체
    @Ignore
    private MyValueAnimator myValueAnimator;
    // 텍스트객체
    @Ignore
    private TextView textView;
    // 현재 hp
    @Ignore
    private int hp;
    // 성 여부
    @Ignore
    private boolean isCastle;
    // 적군 아군 여부
    @Ignore
    private boolean isOur;

    public int getId() {
        return id;
    }

    public void setId(int id) {
        this.id = id;
    }

    public String getName() {
        return name;
    }

    public void setName(String name) {
        this.name = name;
    }

    public int getTotal_hp() {
        return total_hp;
    }

    public void setTotal_hp(int total_hp) {
        this.total_hp = total_hp;
    }

    public int getAttack() {
        return attack;
    }

    public void setAttack(int attack) {
        this.attack = attack;
    }

    public int getRecognize_range() {
        return recognize_range;
    }

    public void setRecognize_range(int recognize_range) {
        this.recognize_range = recognize_range;
    }

    public Long getSpeed() {
        return speed;
    }

    public void setSpeed(Long speed) {
        this.speed = speed;
    }

    public Long getAttack_speed() {
        return attack_speed;
    }

    public void setAttack_speed(Long attack_speed) {
        this.attack_speed = attack_speed;
    }

    public int getType() {
        return type;
    }

    public void setType(int type) {
        this.type = type;
    }

    public int getAttack_style() {
        return attack_style;
    }

    public void setAttack_style(int attack_style) {
        this.attack_style = attack_style;
    }

    public int getAttack_range() {
        return attack_range;
    }

    public void setAttack_range(int attack_range) {
        this.attack_range = attack_range;
    }

    public int getPercent() {
        return percent;
    }

    public void setPercent(int percent) {
        this.percent = percent;
    }

    public ImageView getImageView() {
        return imageView;
    }

    public void setImageView(ImageView imageView) {
        this.imageView = imageView;
    }

    public CountDownTimer getCountDownTimer() {
        return countDownTimer;
    }

    public void setCountDownTimer(CountDownTimer countDownTimer) {
        this.countDownTimer = countDownTimer;
    }

    public MyValueAnimator getMyValueAnimator() {
        return myValueAnimator;
    }

    public void setMyValueAnimator(MyValueAnimator myValueAnimator) {
        this.myValueAnimator = myValueAnimator;
    }

    public TextView getTextView() {
        return textView;
    }

    public void setTextView(TextView textView) {
        this.textView = textView;
    }

    public int getHp() {
        return hp;
    }

    public void setHp(int hp) {
        this.hp = hp;
    }

    public boolean isCastle() {
        return isCastle;
    }

    public void setCastle(boolean castle) {
        isCastle = castle;
    }

    public boolean isOur() {
        return isOur;
    }

    public void setOur(boolean our) {
        isOur = our;
    }

    public int getCost() {
        return cost;
    }

    public void setCost(int cost) {
        this.cost = cost;
    }

    public Monster(String name, int total_hp, int attack, int recognize_range, Long speed, Long attack_speed, int type, int attack_style, int attack_range, int percent, int cost) {
        this.name = name;
        this.total_hp = total_hp;
        this.attack = attack;
        this.recognize_range = recognize_range;
        this.speed = speed;
        this.attack_speed = attack_speed;
        this.type = type;
        this.attack_style = attack_style;
        this.attack_range = attack_range;
        this.percent = percent;
        this.imageView = null;
        this.textView = null;
        this.myValueAnimator = null;
        this.countDownTimer = null;
        this.hp = total_hp;
        this.isCastle = name.indexOf("castle") >= 0;
        this.isOur = name.indexOf("your") >= 0? false : true;
        this.cost = cost;
    }
}
